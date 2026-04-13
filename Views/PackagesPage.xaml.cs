using Microsoft.Management.Deployment;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using RyTuneX.Helpers;
using RyTuneX.Models;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace RyTuneX.Views;

public sealed partial class PackagesPage : Page
{
    private sealed record InstalledPackageEntry(string Id, string Name, string Version, string NormalizedId, string NormalizedName);
    private sealed record DiscoveredPackageEntry(string Id, string Name, string Version);

    public ObservableCollection<WingetPackage> PackageList { get; set; } = new();
    public ObservableCollection<WingetPackage> UpdatesList { get; set; } = new();

    // All packages fetched on load, only rebuilt on Refresh.
    private List<WingetPackage> _allPackages = new();
    private List<WingetPackage> _updateablePackages = new();
    private readonly List<InstalledPackageEntry> _installedSnapshot = new();

    private CancellationTokenSource _cts = new();
    private PackageManager? _packageManager;
    private PackageCatalog? _wingetCatalog;
    private PackageCatalog? _localCatalog;

    private bool? _isWingetAvailable;
    private bool _isUsingInventoryFallback;
    private bool _isUsingCliDiscoveryFallback;
    private bool _isUpdatesMode;
    private bool _isLoading;
    private bool _suppressSearch;
    private int _updateCheckVersion;
    private int _updateCount;

    public PackagesPage()
    {
        InitializeComponent();
        LogHelper.Log("Initializing PackagesPage");
        this.NavigationCacheMode = Microsoft.UI.Xaml.Navigation.NavigationCacheMode.Required;
        Loaded += PackagesPage_Loaded;
    }

    private async void PackagesPage_Loaded(object sender, RoutedEventArgs e)
    {
        if (_isLoading) return;
        if (_allPackages.Count == 0)
            await LoadPackagesAsync();
    }

    // Refresh
    private async void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        if (_isLoading) return;

        // Cancel any running tasks and reset everything
        Interlocked.Increment(ref _updateCheckVersion);
        var old = _cts;
        _cts = new CancellationTokenSource();
        try { old.Cancel(); } catch { }
        old.Dispose();

        _wingetCatalog = null;
        _localCatalog = null;
        _isWingetAvailable = null;
        _isUpdatesMode = false;
        _updateablePackages.Clear();
        _updateCount = 0;
        UpdatesTabLabel.Text = "Updates";
        ApplyTabButtonStyles(browseActive: true);
        PackageSearchBox.Visibility = Visibility.Visible;
        InstallButtonText.Text = "Install Selected";
        InstallButtonIcon.Glyph = "\uE896";
        installingStatusText.Text = "Select a package to install";

        _suppressSearch = true;
        try { PackageSearchBox.Text = string.Empty; }
        finally { _suppressSearch = false; }

        PackageList.Clear();
        UpdatesList.Clear();
        PackagesGridView.Visibility = Visibility.Collapsed;
        UpdatesGridView.Visibility = Visibility.Collapsed;
        LoadingState.Visibility = Visibility.Visible;
        StatusText.Visibility = Visibility.Collapsed;

        await LoadPackagesAsync();
    }

    // Load
    private async Task LoadPackagesAsync()
    {
        _isLoading = true;
        try
        {
            if (!await IsWingetAvailableAsync())
            {
                SetErrorState("Winget is not available on this system.");
                return;
            }

            var catalog = await EnsureWingetCatalogAsync();
            if (catalog is null)
            {
                SetErrorState("Could not connect to the winget source.");
                return;
            }

            _allPackages.Clear();
            PackageList.Clear();
            LoadingState.Visibility = Visibility.Visible;
            PackagesGridView.Visibility = Visibility.Collapsed;

            var installedMap = await GetInstalledPackagesMapAsync();
            var discovered = await DiscoverPackagesAsync(catalog);

            if (discovered.Count < 200)
            {
                await LogHelper.LogWarning("Catalog didn't return enough packages; appending popular-query fallback.");
                var fallback = await DiscoverPopularPackagesFallbackAsync();
                var seenIds = new HashSet<string>(discovered.Select(d => d.Id), StringComparer.OrdinalIgnoreCase);
                foreach (var item in fallback)
                {
                    if (seenIds.Add(item.Id))
                        discovered.Add(item);
                }

                if (discovered.Count == 0)
                {
                    SetErrorState("No packages found. Try Refresh or search by name.");
                    PackagesGridView.Visibility = Visibility.Visible;
                    return;
                }
            }

            int matched = 0;
            foreach (var d in discovered)
            {
                _cts.Token.ThrowIfCancellationRequested();

                var pkg = new WingetPackage
                {
                    Name = d.Name,
                    Id = d.Id,
                    Category = GetPublisherDisplayName(d.Id),
                    Version = d.Version
                };

                (string Name, string Version) inst = default;
                bool isInst = false;

                foreach (var key in GetLookupKeys(pkg.Id, pkg.Name))
                    if (installedMap.TryGetValue(key, out inst)) { isInst = true; break; }

                if (!isInst) isInst = TryGetInstalledByHeuristic(pkg, out inst);

                if (isInst)
                {
                    pkg.IsInstalled = true;
                    matched++;
                    if (!string.IsNullOrWhiteSpace(inst.Version)) pkg.Version = inst.Version;
                    if (!string.IsNullOrWhiteSpace(inst.Name)) pkg.Name = inst.Name;
                }
                else if (string.IsNullOrWhiteSpace(pkg.Version))
                {
                    pkg.Version = "N/A";
                }

                _allPackages.Add(pkg);
            }

            _allPackages = _allPackages
                .OrderBy(p => p.Name, StringComparer.CurrentCultureIgnoreCase)
                .ToList();

            _ = LogHelper.Log($"Loaded {_allPackages.Count} packages, {matched} already installed.");

            // Dump everything into the list
            int count = 0;
            foreach (var p in _allPackages)
            {
                PackageList.Add(p);
                if (++count % 50 == 0) await Task.Delay(1);
            }

            LoadingState.Visibility = Visibility.Collapsed;
            PackagesGridView.Visibility = Visibility.Visible;
            StatusText.Visibility = Visibility.Collapsed;

            if (PackageList.Count == 0) SetErrorState("No packages found.");

            _ = CheckAndApplyUpdatesAsync();
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            _ = LogHelper.LogError($"Error loading packages: {ex.Message}");
            SetErrorState("Failed to load packages.");
        }
        finally
        {
            _isLoading = false;
        }
    }

    // Search 
    private void PackageSearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (_suppressSearch || _isLoading || _isUpdatesMode) return;
        if (args.Reason == AutoSuggestionBoxTextChangeReason.SuggestionChosen) return;
        ApplySearch(sender.Text?.Trim() ?? string.Empty);
    }

    private void PackageSearchBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        if (_suppressSearch || _isLoading || _isUpdatesMode) return;
        ApplySearch(args.QueryText?.Trim() ?? string.Empty);
    }

    private int _searchVersion;

    private async void ApplySearch(string query)
    {
        var currentVersion = Interlocked.Increment(ref _searchVersion);
        PackageList.Clear();

        if (string.IsNullOrWhiteSpace(query))
        {
            // Empty => show everything
            int count = 0;
            foreach (var p in _allPackages)
            {
                if (currentVersion != _searchVersion) return;
                PackageList.Add(p);
                if (++count % 50 == 0) await Task.Delay(1);
            }
        }
        else
        {
            int count = 0;
            foreach (var p in _allPackages)
            {
                if (currentVersion != _searchVersion) return;
                if (p.Name.Contains(query, StringComparison.CurrentCultureIgnoreCase) ||
                    p.Id.Contains(query, StringComparison.CurrentCultureIgnoreCase))
                {
                    PackageList.Add(p);
                    if (++count % 50 == 0) await Task.Delay(1);
                }
            }
        }

        if (currentVersion == _searchVersion)
            _ = LogHelper.Log($"ApplySearch: '{query}' → {PackageList.Count}/{_allPackages.Count}");
    }

    // Update detection
    private async Task CheckAndApplyUpdatesAsync()
    {
        var myVersion = _updateCheckVersion;
        try
        {
            _ = LogHelper.Log("Starting background update check…");
            var updatables = await GetUpdatablePackagesFromCliAsync();
            if (_updateCheckVersion != myVersion) return;
            if (updatables.Count == 0) { _ = LogHelper.Log("No updates found."); return; }

            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var (id, ver) in updatables) map.TryAdd(id, ver);

            var snapshot = _allPackages;
            DispatcherQueue.TryEnqueue(() =>
            {
                if (_updateCheckVersion != myVersion) return;

                int count = 0;
                foreach (var pkg in snapshot)
                {
                    if (map.TryGetValue(pkg.Id, out var latestVer))
                    {
                        pkg.HasUpdate = true;
                        pkg.LatestVersion = latestVer;
                        count++;
                    }
                }

                _updateCount = count;
                _updateablePackages = snapshot.Where(p => p.HasUpdate).ToList();
                UpdatesList.Clear();
                foreach (var pkg in _updateablePackages) UpdatesList.Add(pkg);
                UpdatesTabLabel.Text = count > 0 ? $"Updates ({count})" : "Updates";
                LogHelper.Log($"Update check done — {count} update(s).");

                if (_isUpdatesMode) RefreshUpdatesTabList();
            });
        }
        catch (OperationCanceledException) { }
        catch (Exception ex) { await LogHelper.LogWarning($"Update check failed: {ex.Message}"); }
    }

    private async Task<List<(string Id, string AvailableVersion)>> GetUpdatablePackagesFromCliAsync()
    {
        var psi = new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = "/c winget upgrade --source winget --accept-source-agreements",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        using var process = Process.Start(psi);
        if (process is null) return [];

        var stdOut = process.StandardOutput.ReadToEndAsync();
        var stdErr = process.StandardError.ReadToEndAsync();
        try { await process.WaitForExitAsync(_cts.Token); }
        catch (OperationCanceledException) { TryTerminateProcess(process); throw; }

        var output = await stdOut;
        _ = await stdErr;

        var results = new List<(string, string)>();
        var lines = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        bool headerPassed = false, sepPassed = false;

        foreach (var line in lines)
        {
            if (!headerPassed)
            {
                if (line.Contains("Available", StringComparison.OrdinalIgnoreCase) &&
                    line.Contains("Version", StringComparison.OrdinalIgnoreCase))
                    headerPassed = true;
                continue;
            }
            if (!sepPassed) { if (line.All(c => c == '-' || c == ' ')) { sepPassed = true; continue; } }
            if (line.EndsWith("available.", StringComparison.OrdinalIgnoreCase)) continue;

            var parts = Regex.Split(line, @"\s{2,}");
            if (parts.Length < 4) continue;

            var id = parts[1].Trim();
            var available = parts[3].Trim();
            if (!IsLikelyWingetPackageId(id)) continue;
            if (string.IsNullOrWhiteSpace(available) || available.Equals("Unknown", StringComparison.OrdinalIgnoreCase)) continue;
            results.Add((id, available));
        }

        return results;
    }

    // Tab switching
    private void BrowseTabButton_Click(object sender, RoutedEventArgs e)
    {
        if (!_isUpdatesMode) return;
        _isUpdatesMode = false;
        ApplyTabButtonStyles(browseActive: true);
        PackageSearchBox.Visibility = Visibility.Visible;
        InstallButtonText.Text = "Install Selected";
        InstallButtonIcon.Glyph = "\uE896";
        installingStatusText.Text = "Select a package to install";
        UpdatesGridView.Visibility = Visibility.Collapsed;
        UpdatesGridView.SelectedItems.Clear();
        StatusText.Visibility = Visibility.Collapsed;
        PackagesGridView.Visibility = Visibility.Visible;

        // Re-apply whatever is currently in the search box
        ApplySearch(PackageSearchBox.Text?.Trim() ?? string.Empty);
    }

    private void UpdatesTabButton_Click(object sender, RoutedEventArgs e)
    {
        if (_isUpdatesMode) return;
        _isUpdatesMode = true;
        ApplyTabButtonStyles(browseActive: false);
        PackageSearchBox.Visibility = Visibility.Collapsed;
        InstallButtonText.Text = "Update Selected";
        InstallButtonIcon.Glyph = "\uE898";
        installingStatusText.Text = "Select packages to update";
        PackagesGridView.Visibility = Visibility.Collapsed;
        PackagesGridView.SelectedItems.Clear();
        RefreshUpdatesTabList();
    }

    private void RefreshUpdatesTabList()
    {
        if (UpdatesList.Count != _updateablePackages.Count)
        {
            UpdatesList.Clear();
            foreach (var pkg in _updateablePackages) UpdatesList.Add(pkg);
        }

        if (UpdatesList.Count == 0)
        {
            StatusText.Text = _allPackages.Count == 0
                ? "Loading packages, please wait…"
                : "No updates available. All packages are up to date.";
            StatusText.Visibility = Visibility.Visible;
        }
        else
        {
            StatusText.Visibility = Visibility.Collapsed;
        }

        UpdatesGridView.Visibility = Visibility.Visible;
        PackagesGridView.Visibility = Visibility.Collapsed;
        LoadingState.Visibility = Visibility.Collapsed;
    }

    private void ApplyTabButtonStyles(bool browseActive)
    {
        Style? accent = Application.Current.Resources.TryGetValue("AccentButtonStyle", out var s) ? s as Style : null;
        BrowseTabButton.Style = browseActive ? accent : null;
        UpdatesTabButton.Style = browseActive ? null : accent;
    }

    // Install / Update
    private async void InstallSelectedApp_Click(object sender, RoutedEventArgs e)
    {
        var activeView = _isUpdatesMode ? UpdatesGridView : PackagesGridView;
        var selected = activeView.SelectedItems.Cast<WingetPackage>()
            .Where(p => _isUpdatesMode ? p.HasUpdate : !p.IsInstalled).ToList();

        if (selected.Count == 0)
        {
            ShellPage.ShowNotification("Packages",
                $"No packages selected for {(_isUpdatesMode ? "update" : "install")}.",
                InfoBarSeverity.Warning);
            return;
        }

        InstallButton.IsEnabled = false;
        RefreshButton.IsEnabled = false;
        activeView.IsEnabled = false;
        installingStatusBar.Opacity = 1;
        installingStatusBar.Maximum = selected.Count;
        installingStatusBar.Value = 0;

        int ok = 0, fail = 0;

        for (int i = 0; i < selected.Count; i++)
        {
            var pkg = selected[i];
            bool upgrade = pkg.HasUpdate;
            installingStatusText.Text = $"{(upgrade ? "Updating" : "Installing")} {pkg.Name} ({i + 1}/{selected.Count})…";

            try
            {
                string cmd = upgrade
                    ? $"upgrade --id \"{pkg.Id}\" --exact"
                    : $"install --id \"{pkg.Id}\" --exact";

                var psi = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c winget {cmd} --accept-package-agreements --accept-source-agreements --silent",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8
                };

                using var p = Process.Start(psi);
                if (p != null)
                {
                    await p.WaitForExitAsync(_cts.Token);
                    if (p.ExitCode == 0)
                    {
                        ok++;
                        pkg.IsInstalled = true;
                        if (upgrade)
                        {
                            pkg.Version = pkg.LatestVersion;
                            pkg.HasUpdate = false;
                            pkg.LatestVersion = string.Empty;
                            _updateablePackages.Remove(pkg);
                            _updateCount = Math.Max(0, _updateCount - 1);
                            UpdatesTabLabel.Text = _updateCount > 0 ? $"Updates ({_updateCount})" : "Updates";
                            UpdatesList.Remove(pkg);
                        }
                    }
                    else
                    {
                        fail++;
                        await LogHelper.LogWarning($"Failed to {(upgrade ? "upgrade" : "install")} {pkg.Name}. Exit: {p.ExitCode}");
                    }
                }
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                fail++;
                await LogHelper.LogError($"Exception on {pkg.Name}: {ex.Message}");
            }

            installingStatusBar.Value = i + 1;
        }

        InstallButton.IsEnabled = true;
        RefreshButton.IsEnabled = true;
        activeView.IsEnabled = true;
        installingStatusText.Text = _isUpdatesMode ? "Select packages to update" : "Select a package to install";
        installingStatusBar.Opacity = 0;
        installingStatusBar.Value = 0;
        activeView.SelectedItems.Clear();

        if (_isUpdatesMode && UpdatesList.Count == 0)
        {
            StatusText.Text = "All updates installed successfully.";
            StatusText.Visibility = Visibility.Visible;
        }

        ShellPage.ShowNotification("Packages",
            $"{(_isUpdatesMode ? "Update" : "Installation")} completed: {ok} succeeded, {fail} failed.",
            fail == 0 ? InfoBarSeverity.Success : InfoBarSeverity.Warning);
    }

    // SelectionChanged
    private void PackagesGridView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is not ListViewBase view) return;

        foreach (WingetPackage item in e.AddedItems)
            if (item.IsInstalled && !item.HasUpdate)
                view.SelectedItems.Remove(item);

        var count = view.SelectedItems.Count;
        installingStatusText.Text = count == 0
            ? (_isUpdatesMode ? "Select packages to update" : "Select a package to install")
            : $"{count} package{(count == 1 ? "" : "s")} selected for {(_isUpdatesMode ? "update" : "installation")}";
    }

    // Winget discovery
    private async Task<bool> IsWingetAvailableAsync()
    {
        if (_isWingetAvailable.HasValue) return _isWingetAvailable.Value;
        try { _isWingetAvailable = await EnsureWingetCatalogAsync() is not null; return _isWingetAvailable.Value; }
        catch { }
        _isWingetAvailable = false;
        return false;
    }

    private async Task<List<DiscoveredPackageEntry>> DiscoverPackagesAsync(PackageCatalog catalog)
    {
        _isUsingCliDiscoveryFallback = false;
        try
        {
            var packages = new List<DiscoveredPackageEntry>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            // Winget's API treats ResultLimit = 0 as "use default server limit". To remove the limit, use a massive number.
            var result = await catalog.FindPackagesAsync(new FindPackagesOptions { ResultLimit = int.MaxValue });

            foreach (var match in result.Matches)
            {
                var cp = match.CatalogPackage;
                var id = cp.Id?.Trim() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(id) || !seen.Add(id)) continue;
                var name = string.IsNullOrWhiteSpace(cp.Name) ? FormatPackageName(id) : cp.Name;
                var ver = cp.AvailableVersions.FirstOrDefault()?.Version;
                packages.Add(new DiscoveredPackageEntry(id, name, string.IsNullOrWhiteSpace(ver) ? "N/A" : ver));
            }

            return packages;
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex) when (ex.Message.Contains("No such interface supported", StringComparison.OrdinalIgnoreCase))
        {
            _isUsingCliDiscoveryFallback = true;
            await LogHelper.LogWarning($"COM catalog unavailable, falling back to CLI. {ex.Message}");
            return await DiscoverPackagesFromWingetCliAsync();
        }
        catch (Exception ex)
        {
            await LogHelper.LogError($"Package discovery failed: {ex.Message}");
            return [];
        }
    }

    private async Task<List<DiscoveredPackageEntry>> DiscoverPackagesFromWingetCliAsync()
    {
        _isUsingCliDiscoveryFallback = true;
        var psi = new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = "/c winget search --accept-source-agreements",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        using var process = Process.Start(psi);
        if (process is null) { await LogHelper.LogError("Failed to start winget CLI."); return []; }

        var stdOut = process.StandardOutput.ReadToEndAsync();
        var stdErr = process.StandardError.ReadToEndAsync();
        try { await process.WaitForExitAsync(_cts.Token); }
        catch (OperationCanceledException) { TryTerminateProcess(process); throw; }

        var output = await stdOut;
        _ = await stdErr;
        return process.ExitCode != 0 ? [] : ParseWingetSearchOutput(output);
    }

    private async Task<List<DiscoveredPackageEntry>> DiscoverPopularPackagesFallbackAsync()
    {
        var results = new List<DiscoveredPackageEntry>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var q in new[] { "browser", "media", "code", "chat", "game", "archive", "social", "utility", "system" })
            foreach (var item in await SearchPackagesFromWingetCliAsync(q))
                if (seen.Add(item.Id)) results.Add(item);

        return results;
    }

    private async Task<List<DiscoveredPackageEntry>> SearchPackagesFromWingetCliAsync(
        string query, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query)) return [];
        var token = cancellationToken.CanBeCanceled ? cancellationToken : _cts.Token;

        var psi = new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = $"/c winget search --query \"{query.Replace("\"", "\"\"")}\" --source winget --accept-source-agreements",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        using var process = Process.Start(psi);
        if (process is null) return [];

        var stdOut = process.StandardOutput.ReadToEndAsync();
        var stdErr = process.StandardError.ReadToEndAsync();
        try { await process.WaitForExitAsync(token); }
        catch (OperationCanceledException) { TryTerminateProcess(process); throw; }

        var output = await stdOut;
        _ = await stdErr;
        return process.ExitCode != 0 ? [] : ParseWingetSearchOutput(output);
    }

    private static List<DiscoveredPackageEntry> ParseWingetSearchOutput(string output)
    {
        var packages = new List<DiscoveredPackageEntry>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var line in output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (line.StartsWith("The `msstore`", StringComparison.OrdinalIgnoreCase) ||
                line.StartsWith("Do you agree", StringComparison.OrdinalIgnoreCase)) continue;
            if (line.StartsWith("Name", StringComparison.OrdinalIgnoreCase) && line.Contains("Id")) continue;
            if (line.All(c => c == '-' || c == ' ')) continue;

            var parts = Regex.Split(line, @"\s{2,}");
            if (parts.Length < 2) continue;

            var id = parts[1].Trim();
            if (!IsLikelyWingetPackageId(id) || !seen.Add(id)) continue;

            var name = string.IsNullOrWhiteSpace(parts[0]) ? FormatPackageName(id) : parts[0].Trim();
            var ver = parts.Length > 2 && !string.IsNullOrWhiteSpace(parts[2]) ? parts[2].Trim() : "N/A";
            if (ver.Equals("Unknown", StringComparison.OrdinalIgnoreCase)) ver = "N/A";

            packages.Add(new DiscoveredPackageEntry(id, name, ver));
        }

        return packages;
    }

    // Catalog connections
    private async Task<PackageCatalog?> EnsureWingetCatalogAsync()
    {
        if (_wingetCatalog is not null) return _wingetCatalog;
        try
        {
            var src = await Task.Run(() =>
            {
                _packageManager ??= new PackageManager();
                return _packageManager.GetPackageCatalogByName("winget");
            });
            src.AcceptSourceAgreements = true;
            var r = await Task.Run(() => src.ConnectAsync().AsTask());
            if (r.Status == ConnectResultStatus.Ok) { _wingetCatalog = r.PackageCatalog; return _wingetCatalog; }
            await LogHelper.LogWarning($"Winget source failed: {r.Status}");
        }
        catch (Exception ex) { await LogHelper.LogError($"Winget connect error: {ex.Message}"); }
        return null;
    }

    private async Task<PackageCatalog?> EnsureLocalCatalogAsync()
    {
        if (_localCatalog is not null) return _localCatalog;
        try
        {
            var src = await Task.Run(() =>
            {
                _packageManager ??= new PackageManager();
                return _packageManager.GetLocalPackageCatalog(LocalPackageCatalog.InstalledPackages);
            });
            var r = await Task.Run(() => src.ConnectAsync().AsTask());
            if (r.Status == ConnectResultStatus.Ok) { _localCatalog = r.PackageCatalog; return _localCatalog; }
            await LogHelper.LogWarning($"Local catalog failed: {r.Status}");
        }
        catch (Exception ex) { await LogHelper.LogError($"Local catalog connect error: {ex.Message}"); }
        return null;
    }

    // Installed detection
    private async Task<Dictionary<string, (string Name, string Version)>> GetInstalledPackagesMapAsync()
    {
        var result = new Dictionary<string, (string Name, string Version)>(StringComparer.OrdinalIgnoreCase);
        _installedSnapshot.Clear();
        _isUsingInventoryFallback = false;

        try
        {
            var local = await EnsureLocalCatalogAsync();
            if (local is null) return result;

            foreach (var match in (await local.FindPackagesAsync(new FindPackagesOptions())).Matches)
            {
                var ip = match.CatalogPackage;
                var ver = ip.InstalledVersion?.Version ?? ip.AvailableVersions.FirstOrDefault()?.Version ?? string.Empty;
                var iid = ip.Id ?? string.Empty;
                var iname = ip.Name ?? string.Empty;

                _installedSnapshot.Add(new InstalledPackageEntry(
                    iid, iname, ver, NormalizeLookupKey(iid), NormalizeLookupKey(iname)));

                if (!string.IsNullOrWhiteSpace(iid))
                {
                    result[iid] = (iname, ver);
                    foreach (var key in GetLookupKeys(iid, iname))
                        result.TryAdd(key, (iname, ver));
                }
                if (!string.IsNullOrWhiteSpace(iname))
                    result.TryAdd(NormalizeLookupKey(iname), (iname, ver));
            }
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            await LogHelper.LogWarning($"Local catalog query failed: {ex.Message}");
            if (ex.Message.Contains("No such interface supported", StringComparison.OrdinalIgnoreCase))
                await PopulateInstalledMapFallbackAsync(result);
        }

        return result;
    }

    private async Task PopulateInstalledMapFallbackAsync(Dictionary<string, (string Name, string Version)> result)
    {
        try
        {
            _isUsingInventoryFallback = true;
            var (apps, _) = await OptimizationOptions.GetInstalledApps();
            foreach (var app in apps)
            {
                if (string.IsNullOrWhiteSpace(app.Item1)) continue;
                var name = app.Item1.Trim();
                var key = NormalizeLookupKey(name);
                if (string.IsNullOrWhiteSpace(key)) continue;
                result.TryAdd(key, (name, "Installed"));
                _installedSnapshot.Add(new InstalledPackageEntry(string.Empty, name, "Installed", string.Empty, key));
            }
            _ = LogHelper.Log($"Inventory fallback: {_installedSnapshot.Count} installed apps.");
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex) { await LogHelper.LogWarning($"Inventory fallback failed: {ex.Message}"); }
    }

    private bool TryGetInstalledByHeuristic(WingetPackage pkg, out (string Name, string Version) installed)
    {
        installed = default;
        if (_installedSnapshot.Count == 0) return false;

        var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var key in GetLookupKeys(pkg.Id, pkg.Name))
        {
            var n = NormalizeLookupKey(key);
            if (!string.IsNullOrWhiteSpace(n)) keys.Add(n);
        }
        if (keys.Count == 0) return false;

        foreach (var c in _installedSnapshot)
        {
            if (!string.IsNullOrWhiteSpace(c.NormalizedName) && keys.Contains(c.NormalizedName)) { installed = (c.Name, c.Version); return true; }
            if (!string.IsNullOrWhiteSpace(c.NormalizedId) && keys.Contains(c.NormalizedId)) { installed = (c.Name, c.Version); return true; }
            foreach (var key in keys)
            {
                if (key.Length < 6 || string.IsNullOrWhiteSpace(c.NormalizedName)) continue;
                if (c.NormalizedName.EndsWith(key, StringComparison.Ordinal)) { installed = (c.Name, c.Version); return true; }
            }
        }

        return false;
    }

    // Helpers
    private static string FormatPackageName(string id)
    {
        var d = id.IndexOf('.');
        return d >= 0 ? id[(d + 1)..].Replace('.', ' ') : id;
    }

    private static string GetPublisherDisplayName(string id)
    {
        var d = id.IndexOf('.');
        return d <= 0 ? "Unknown" : id[..d];
    }

    private static bool IsLikelyWingetPackageId(string v) =>
        !string.IsNullOrWhiteSpace(v) && !v.Contains(' ') && v.Length >= 3 && (v.Contains('.') || v.Contains('-'));

    private static string NormalizeLookupKey(string value)
    {
        var sb = new StringBuilder(value.Length);
        foreach (var c in value) if (char.IsLetterOrDigit(c)) sb.Append(char.ToLowerInvariant(c));
        return sb.ToString();
    }

    private static IEnumerable<string> GetLookupKeys(string id, string name)
    {
        var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        void Add(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return;
            keys.Add(raw);
            var n = NormalizeLookupKey(raw);
            if (!string.IsNullOrWhiteSpace(n)) keys.Add(n);
        }
        Add(id); Add(name); Add(FormatPackageName(id));
        var f = id.IndexOf('.'); if (f >= 0 && f + 1 < id.Length) Add(id[(f + 1)..]);
        var l = id.LastIndexOf('.'); if (l >= 0 && l + 1 < id.Length) Add(id[(l + 1)..]);
        return keys;
    }

    private void SetErrorState(string message)
    {
        LoadingState.Visibility = Visibility.Collapsed;
        StatusText.Text = message;
        StatusText.Visibility = Visibility.Visible;
        _isLoading = false;
    }

    private static void TryTerminateProcess(Process p)
    {
        try { if (!p.HasExited) p.Kill(entireProcessTree: true); } catch { }
    }

    private static T? FindDescendant<T>(DependencyObject root) where T : DependencyObject
    {
        if (root is T t) return t;
        var count = VisualTreeHelper.GetChildrenCount(root);
        for (var i = 0; i < count; i++)
        {
            var m = FindDescendant<T>(VisualTreeHelper.GetChild(root, i));
            if (m is not null) return m;
        }
        return null;
    }
}
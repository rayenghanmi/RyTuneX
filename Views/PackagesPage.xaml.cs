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
    private bool? _isWingetAvailable;
    private bool _isUpdatesMode;
    private bool _isLoading;
    private bool _isOperating;   // true while an install/upgrade is running
    private bool _isPageLoaded;  // true once Loaded is called + guards against InitializeComponent events
    private bool _suppressSearch;
    private int _updateCheckVersion;
    private int _updateCount;
    private int _searchVersion;

    public PackagesPage()
    {
        InitializeComponent();
        LogHelper.Log("Initializing PackagesPage");
        this.NavigationCacheMode = Microsoft.UI.Xaml.Navigation.NavigationCacheMode.Required;
        Loaded += PackagesPage_Loaded;
    }

    private async void PackagesPage_Loaded(object sender, RoutedEventArgs e)
    {
        _isPageLoaded = true;
        if (_isLoading || _isOperating) return;
        if (_allPackages.Count == 0)
            await LoadPackagesAsync();
    }

    // Enable / disable the whole page UI

    private void SetPageBusy(bool busy)
    {
        InstallButton.IsEnabled = !busy;
        RefreshButton.IsEnabled = !busy;
        TabSegmented.IsEnabled = !busy;
        PackageSearchBox.IsEnabled = !busy;
    }

    private void SetListBusy(bool busy)
    {
        PackagesGridView.IsEnabled = !busy;
        UpdatesGridView.IsEnabled = !busy;
    }

    // Refresh

    private async void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        if (_isLoading || _isOperating) return;

        // Cancel any running background tasks and reset version counters
        Interlocked.Increment(ref _updateCheckVersion);
        Interlocked.Increment(ref _searchVersion);
        var old = _cts;
        _cts = new CancellationTokenSource();
        try { old.Cancel(); } catch { }
        old.Dispose();

        _isWingetAvailable = null;
        _isUpdatesMode = false;
        _allPackages.Clear();
        _updateablePackages.Clear();
        _installedSnapshot.Clear();
        _updateCount = 0;

        // Reset tab selection: set _isUpdatesMode = false first
        // Then manually restore all UI to the Browse/loading state
        UpdatesTabLabel.Text = "PackagesPage_UpdatesTabLabel.Text".GetLocalized();
        TabSegmented.SelectedIndex = 0;

        // Reset all visibility here because SelectionChanged bails early
        // when _isUpdatesMode is already false
        PackageSearchBox.Visibility = Visibility.Visible;
        InstallButtonText.Text = "PackagesPage_InstallButton.Text".GetLocalized();
        InstallButtonIcon.Glyph = "\uE896";
        installingStatusText.Text = "PackagesPage_SelectHint.Text".GetLocalized();

        _suppressSearch = true;
        try { PackageSearchBox.Text = string.Empty; }
        finally { _suppressSearch = false; }

        PackageList.Clear();
        UpdatesList.Clear();
        UpdatesGridView.Visibility = Visibility.Collapsed;
        UpdatesGridView.SelectedItems.Clear();
        PackagesGridView.Visibility = Visibility.Collapsed;
        PackagesGridView.SelectedItems.Clear();
        LoadingState.Visibility = Visibility.Visible;
        StatusText.Visibility = Visibility.Collapsed;

        await LoadPackagesAsync();
    }

    // Load

    private async Task LoadPackagesAsync()
    {
        _isLoading = true;
        SetPageBusy(true);
        SetListBusy(true);

        try
        {
            if (!await IsWingetAvailableAsync())
            {
                SetErrorState("Winget is not available on this system.");
                return;
            }

            _allPackages.Clear();
            PackageList.Clear();
            LoadingState.Visibility = Visibility.Visible;
            PackagesGridView.Visibility = Visibility.Collapsed;

            var installedMap = await GetInstalledPackagesMapAsync();
            var discovered = await DiscoverPackagesFromWingetCliAsync();

            if (discovered.Count < 200)
            {
                await LogHelper.LogWarning("Catalog didn't return enough packages; appending popular-query fallback.");
                var fallback = await DiscoverPopularPackagesFallbackAsync();
                var seenIds = new HashSet<string>(discovered.Select(d => d.Id), StringComparer.OrdinalIgnoreCase);
                foreach (var item in fallback)
                    if (seenIds.Add(item.Id))
                        discovered.Add(item);

                if (discovered.Count == 0)
                {
                    SetErrorState("No packages found. Try Refresh or search by name.");
                    return;
                }
            }

            // Build _allPackages on the current UI thread context
            int matched = 0;
            var built = new List<WingetPackage>(discovered.Count);

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

                built.Add(pkg);
            }

            built.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.CurrentCultureIgnoreCase));
            _allPackages = built;

            _ = LogHelper.Log($"Loaded {_allPackages.Count} packages, {matched} already installed.");

            // Feed into the ObservableCollection in batches to keep the UI responsive
            var currentSearch = _searchVersion;
            int batchCount = 0;
            foreach (var p in _allPackages)
            {
                _cts.Token.ThrowIfCancellationRequested();
                if (_searchVersion != currentSearch) break; // search was started, stop bulk add
                PackageList.Add(p);
                if (++batchCount % 100 == 0) await Task.Delay(1);
            }

            LoadingState.Visibility = Visibility.Collapsed;
            PackagesGridView.Visibility = Visibility.Visible;
            StatusText.Visibility = Visibility.Collapsed;

            if (PackageList.Count == 0) SetErrorState("No packages found.");

            // Kick off the update check in the background
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
            // Only re-enable page controls when not in the middle of an install
            if (!_isOperating)
            {
                SetPageBusy(false);
                SetListBusy(false);
            }
        }
    }

    // Search

    private void PackageSearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (_suppressSearch || _isLoading || _isUpdatesMode) return;
        if (args.Reason == AutoSuggestionBoxTextChangeReason.SuggestionChosen) return;
        _ = ApplySearch(sender.Text?.Trim() ?? string.Empty);
    }

    private void PackageSearchBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        if (_suppressSearch || _isLoading || _isUpdatesMode) return;
        _ = ApplySearch(args.QueryText?.Trim() ?? string.Empty);
    }

    private async Task ApplySearch(string query)
    {
        // Stamp this search, any older in-flight batch will see a mismatch and abort
        var myVersion = Interlocked.Increment(ref _searchVersion);
        PackageList.Clear();

        if (string.IsNullOrWhiteSpace(query))
        {
            // Empty query: show all packages. No async batching needed
            // just fill synchronously so the caller can await completion
            foreach (var p in _allPackages)
            {
                if (myVersion != _searchVersion) return;
                PackageList.Add(p);
            }
        }
        else
        {
            int batchCount = 0;
            foreach (var p in _allPackages)
            {
                if (myVersion != _searchVersion) return;

                if (p.Name.Contains(query, StringComparison.CurrentCultureIgnoreCase) ||
                    p.Id.Contains(query, StringComparison.CurrentCultureIgnoreCase))
                {
                    PackageList.Add(p);
                    if (++batchCount % 100 == 0) await Task.Delay(1);
                }
            }
        }

        if (myVersion == _searchVersion)
            _ = LogHelper.Log($"ApplySearch: '{query}' → {PackageList.Count}/{_allPackages.Count}");
    }

    // Update detection

    private async Task CheckAndApplyUpdatesAsync()
    {
        var myVersion = _updateCheckVersion;
        // Snapshot the token now before any await so a later Refresh that
        // disposes the old CTS cannot cause ObjectDisposedException
        var token = _cts.Token;
        try
        {
            await LogHelper.Log("Starting background update check…");
            var updatables = await GetUpdatablePackagesFromCliAsync(token);
            if (_updateCheckVersion != myVersion) return;
            if (updatables.Count == 0) { await LogHelper.Log("No updates found."); return; }

            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var (id, ver) in updatables) map.TryAdd(id, ver);

            // Capture snapshot reference before dispatch
            var snapshot = _allPackages;
            DispatcherQueue.TryEnqueue(() =>
            {
                try
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
                    _updateablePackages = [.. snapshot.Where(p => p.HasUpdate)];

                    // Rebuild UpdatesList in one shot
                    UpdatesList.Clear();
                    foreach (var pkg in _updateablePackages) UpdatesList.Add(pkg);

                    var baseLabel = "PackagesPage_UpdatesTabLabel.Text".GetLocalized();
                    UpdatesTabLabel.Text = count > 0 ? $"{baseLabel} ({count})" : baseLabel;
                    _ = LogHelper.Log($"Update check done — {count} update(s).");

                    if (_isUpdatesMode) RefreshUpdatesTabList();
                }
                catch (Exception ex)
                {
                    _ = LogHelper.LogError($"Error applying updates to UI: {ex.Message}");
                }
            });
        }
        catch (OperationCanceledException) { }
        catch (Exception ex) { await LogHelper.LogWarning($"Update check failed: {ex.Message}"); }
    }

    private async Task<List<(string Id, string AvailableVersion)>> GetUpdatablePackagesFromCliAsync(
        CancellationToken token)
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

        // Read both streams concurrently with the process running so the pipe
        // buffers never fill and deadlock the process on large output
        var stdOutTask = process.StandardOutput.ReadToEndAsync(token);
        var stdErrTask = process.StandardError.ReadToEndAsync(token);

        try
        {
            await Task.WhenAll(stdOutTask, stdErrTask, process.WaitForExitAsync(token));
        }
        catch (OperationCanceledException)
        {
            TryTerminateProcess(process);
            throw;
        }

        if (!stdOutTask.IsCompletedSuccessfully) return [];
        var output = stdOutTask.Result;

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

    private async void TabSegmented_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // The Segmented control fires SelectionChanged during InitializeComponent (SelectedIndex=0)
        // before named elements are ready. Bail out until the page is fully loaded
        if (!_isPageLoaded) return;

        // Block tab switches during any operation or while loading
        if (_isLoading || _isOperating) { TabSegmented.SelectedIndex = _isUpdatesMode ? 1 : 0; return; }

        if (TabSegmented.SelectedIndex == 0)
        {
            // Always restore Browse state, regardless of _isUpdatesMode, so that
            // Refresh (which resets _isUpdatesMode before changing the tab index)
            // also correctly hides the Updates grid
            _isUpdatesMode = false;

            PackageSearchBox.Visibility = Visibility.Visible;
            InstallButtonText.Text = "PackagesPage_InstallButton.Text".GetLocalized();
            InstallButtonIcon.Glyph = "\uE896";
            installingStatusText.Text = "PackagesPage_SelectHint.Text".GetLocalized();

            // Always hide Updates grid and show the Browse grid
            UpdatesGridView.Visibility = Visibility.Collapsed;
            UpdatesGridView.SelectedItems.Clear();
            StatusText.Visibility = Visibility.Collapsed;
            LoadingState.Visibility = Visibility.Collapsed;

            // Keep PackagesGridView hidden while repopulating PackageList
            // Showing it before the fill completes causes the stale updates list
            // (or an empty/partially-cleared list) to flash on screen
            PackagesGridView.Visibility = Visibility.Collapsed;
            await ApplySearch(PackageSearchBox.Text?.Trim() ?? string.Empty);

            // Only show the list if there's actually something to display
            if (PackageList.Count > 0)
                PackagesGridView.Visibility = Visibility.Visible;
            else if (_allPackages.Count > 0)
                SetErrorState("No packages match your search.");
        }
        else if (TabSegmented.SelectedIndex == 1)
        {
            if (_isUpdatesMode) return;
            _isUpdatesMode = true;

            PackageSearchBox.Visibility = Visibility.Collapsed;
            InstallButtonText.Text = "PackagesPage_UpdateButton".GetLocalized();
            InstallButtonIcon.Glyph = "\uE898";
            installingStatusText.Text = "PackagesPage_SelectUpdateHint".GetLocalized();

            PackagesGridView.Visibility = Visibility.Collapsed;
            PackagesGridView.SelectedItems.Clear();
            RefreshUpdatesTabList();
        }
    }

    private void RefreshUpdatesTabList()
    {
        // Sync UpdatesList with _updateablePackages if out of date
        if (UpdatesList.Count != _updateablePackages.Count)
        {
            UpdatesList.Clear();
            foreach (var pkg in _updateablePackages) UpdatesList.Add(pkg);
        }

        if (UpdatesList.Count == 0)
        {
            StatusText.Text = _allPackages.Count == 0
                ? "PackagesPage_StatusLoading".GetLocalized()
                : "PackagesPage_StatusNoUpdates".GetLocalized();
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

    // Install / Update

    private async void InstallSelectedApp_Click(object sender, RoutedEventArgs e)
    {
        if (_isLoading || _isOperating) return;

        var activeView = _isUpdatesMode ? UpdatesGridView : PackagesGridView;
        var selected = activeView.SelectedItems.Cast<WingetPackage>()
            .Where(p => _isUpdatesMode ? p.HasUpdate : !p.IsInstalled)
            .ToList();

        if (selected.Count == 0)
        {
            var noSelMsg = _isUpdatesMode
                ? "PackagesPage_Notification_NoPackagesUpdate".GetLocalized()
                : "PackagesPage_Notification_NoPackagesInstall".GetLocalized();
            ShellPage.ShowNotification(
                "PackagesPage_Notification_Title".GetLocalized(),
                noSelMsg,
                InfoBarSeverity.Warning);
            return;
        }

        _isOperating = true;
        SetPageBusy(true);
        SetListBusy(true);

        installingStatusBar.Opacity = 1;
        installingStatusBar.Maximum = selected.Count;
        installingStatusBar.Value = 0;

        OperationCancellationManager.EnterOperation();
        var localCts = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token);
        var opId = OperationCancellationManager.Register(localCts);

        int ok = 0, fail = 0;

        try
        {
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
                        // Drain output asynchronously so the process never blocks on full pipe buffers
                        var outTask = p.StandardOutput.ReadToEndAsync();
                        var errTask = p.StandardError.ReadToEndAsync();

                        try { await p.WaitForExitAsync(localCts.Token); }
                        catch (OperationCanceledException) { TryTerminateProcess(p); break; }

                        _ = await outTask;
                        _ = await errTask;

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

                                var baseLabel = "PackagesPage_UpdatesTabLabel.Text".GetLocalized();
                                UpdatesTabLabel.Text = _updateCount > 0
                                    ? $"{baseLabel} ({_updateCount})"
                                    : baseLabel;
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
        }
        finally
        {
            OperationCancellationManager.Unregister(opId);
            localCts.Dispose();
            OperationCancellationManager.ExitOperation();

            _isOperating = false;
            SetPageBusy(false);
            SetListBusy(false);

            installingStatusText.Text = _isUpdatesMode
                ? "PackagesPage_SelectUpdateHint".GetLocalized()
                : "PackagesPage_SelectHint.Text".GetLocalized();
            installingStatusBar.Opacity = 0;
            installingStatusBar.Value = 0;
            activeView.SelectedItems.Clear();

            if (_isUpdatesMode && UpdatesList.Count == 0)
            {
                StatusText.Text = "PackagesPage_StatusNoUpdates".GetLocalized();
                StatusText.Visibility = Visibility.Visible;
            }

            var compMsg = _isUpdatesMode
                ? string.Format("PackagesPage_Notification_UpdateCompleted".GetLocalized(), ok, fail)
                : string.Format("PackagesPage_Notification_InstallCompleted".GetLocalized(), ok, fail);
            ShellPage.ShowNotification(
                "PackagesPage_Notification_Title".GetLocalized(),
                compMsg,
                fail == 0 ? InfoBarSeverity.Success : InfoBarSeverity.Warning);
        }
    }

    // SelectionChanged

    private void PackagesGridView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is not ListViewBase view) return;
        if (_isLoading) return;  // Ignore selection events during bulk-load

        // Deselect items that cannot be acted on
        foreach (WingetPackage item in e.AddedItems)
            if (item.IsInstalled && !item.HasUpdate)
                view.SelectedItems.Remove(item);

        var count = view.SelectedItems.Count;
        installingStatusText.Text = count == 0
            ? (_isUpdatesMode
                ? "PackagesPage_SelectUpdateHint".GetLocalized()
                : "PackagesPage_SelectHint.Text".GetLocalized())
            : string.Format(
                _isUpdatesMode
                    ? "PackagesPage_SelectedForUpdate".GetLocalized()
                    : "PackagesPage_SelectedForInstall".GetLocalized(),
                count);
    }

    // Winget discovery

    private async Task<bool> IsWingetAvailableAsync()
    {
        if (_isWingetAvailable.HasValue) return _isWingetAvailable.Value;
        try
        {
            _isWingetAvailable = await IsWingetCliAvailableAsync();
            return _isWingetAvailable.Value;
        }
        catch { }

        _isWingetAvailable = false;
        return false;
    }

    private async Task<bool> IsWingetCliAvailableAsync()
    {
        var psi = new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = "/c winget --version",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        using var process = Process.Start(psi);
        if (process is null) return false;

        var stdOut = process.StandardOutput.ReadToEndAsync();
        var stdErr = process.StandardError.ReadToEndAsync();
        try { await process.WaitForExitAsync(_cts.Token); }
        catch (OperationCanceledException) { TryTerminateProcess(process); throw; }

        _ = await stdOut;
        _ = await stdErr;

        return process.ExitCode == 0;
    }

    private async Task<List<DiscoveredPackageEntry>> DiscoverPackagesFromWingetCliAsync()
    {
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

        // Run fallback queries sequentially so we don't hammer the network
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

    // Installed detection

    private async Task<Dictionary<string, (string Name, string Version)>> GetInstalledPackagesMapAsync()
    {
        var result = new Dictionary<string, (string Name, string Version)>(StringComparer.OrdinalIgnoreCase);
        _installedSnapshot.Clear();
        await PopulateInstalledMapFallbackAsync(result);
        return result;
    }

    private async Task PopulateInstalledMapFallbackAsync(Dictionary<string, (string Name, string Version)> result)
    {
        try
        {
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
            if (!string.IsNullOrWhiteSpace(c.NormalizedName) && keys.Contains(c.NormalizedName))
            { installed = (c.Name, c.Version); return true; }
            if (!string.IsNullOrWhiteSpace(c.NormalizedId) && keys.Contains(c.NormalizedId))
            { installed = (c.Name, c.Version); return true; }
            foreach (var key in keys)
            {
                if (key.Length < 6 || string.IsNullOrWhiteSpace(c.NormalizedName)) continue;
                if (c.NormalizedName.EndsWith(key, StringComparison.Ordinal))
                { installed = (c.Name, c.Version); return true; }
            }
        }

        return false;
    }

    // Static helpers

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

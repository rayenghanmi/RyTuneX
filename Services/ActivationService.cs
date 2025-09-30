using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using RyTuneX.Activation;
using RyTuneX.Contracts.Services;
using RyTuneX.Views;

namespace RyTuneX.Services;

public class ActivationService : IActivationService
{
    private readonly ActivationHandler<LaunchActivatedEventArgs> _defaultHandler;
    private readonly IEnumerable<IActivationHandler> _activationHandlers;
    private readonly IThemeSelectorService _themeSelectorService;
    private UIElement? _shell = null;
    private bool _initializedShell = false;

    public ActivationService(ActivationHandler<LaunchActivatedEventArgs> defaultHandler, IEnumerable<IActivationHandler> activationHandlers, IThemeSelectorService themeSelectorService)
    {
        _defaultHandler = defaultHandler;
        _activationHandlers = activationHandlers;
        _themeSelectorService = themeSelectorService;
    }

    public async Task ActivateAsync(object activationArgs)
    {
        await InitializeAsync(); // only load theme

        // Present an empty frame immediately to show a window asap
        if (App.MainWindow.Content == null)
        {
            App.MainWindow.Content = new Frame();
        }

        App.MainWindow.Activate();

        // Defer the rest of the UI intialization to show the window faster
        App.MainWindow.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, async () =>
        {
            await EnsureShellAsync();
            await HandleActivationAsync(activationArgs);
            await StartupAsync();
        });
    }

    private async Task EnsureShellAsync()
    {
        if (_initializedShell)
        {
            return;
        }

        _shell = App.GetService<ShellPage>();
        App.MainWindow.Content = _shell;
        _initializedShell = true;
    }

    private async Task HandleActivationAsync(object activationArgs)
    {
        var activationHandler = _activationHandlers.FirstOrDefault(h => h.CanHandle(activationArgs));
        if (activationHandler != null)
        {
            await activationHandler.HandleAsync(activationArgs);
        }
        if (_defaultHandler.CanHandle(activationArgs))
        {
            await _defaultHandler.HandleAsync(activationArgs);
        }
    }

    private async Task InitializeAsync()
    {
        await _themeSelectorService.InitializeAsync().ConfigureAwait(false);
        await Task.CompletedTask;
    }

    private async Task StartupAsync()
    {
        await _themeSelectorService.SetRequestedThemeAsync();
        await Task.CompletedTask;
    }
}
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;
using Microsoft.Windows.AppNotifications;

using RyTuneX.Contracts.Services;
using RyTuneX.Helpers;
using RyTuneX.ViewModels;

namespace RyTuneX.Activation;

public class AppNotificationActivationHandler : ActivationHandler<LaunchActivatedEventArgs>
{
    private readonly INavigationService _navigationService;
    private readonly IAppNotificationService _notificationService;

    public AppNotificationActivationHandler(INavigationService navigationService, IAppNotificationService notificationService)
    {
        _navigationService = navigationService;
        _notificationService = notificationService;
    }

    protected override bool CanHandleInternal(LaunchActivatedEventArgs args)
    {
        return AppInstance.GetCurrent().GetActivatedEventArgs()?.Kind == ExtendedActivationKind.AppNotification;
    }

    protected async override Task HandleInternalAsync(LaunchActivatedEventArgs args)
    {
        // TODO: Handle notification activations.

        //// // Access the AppNotificationActivatedEventArgs.
        //// var activatedEventArgs = (AppNotificationActivatedEventArgs)AppInstance.GetCurrent().GetActivatedEventArgs().Data;

        //// // Navigate to a specific page based on the notification arguments.
        //// if (_notificationService.ParseArguments(activatedEventArgs.Argument)["action"] == "Settings")
        //// {
        ////     // Queue navigation with low priority to allow the UI to initialize.
        ////     App.MainWindow.DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
        ////     {
        ////         _navigationService.NavigateTo(typeof(SettingsViewModel).FullName!);
        ////     });
        //// }

        App.MainWindow.DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
        {
            var welcomeMessage = "WelcomeNotice".GetLocalized().Replace("\\n", Environment.NewLine); ;
            App.MainWindow.ShowMessageDialogAsync(welcomeMessage, "WelcomeNoticeTitle".GetLocalized());
        });

        await Task.CompletedTask;
    }
}

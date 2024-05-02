using System.Collections.Specialized;
using System.Web;
using Microsoft.Windows.AppNotifications;

using RyTuneX.Contracts.Services;
using RyTuneX.Helpers;

namespace RyTuneX.Notifications;

public class AppNotificationService : IAppNotificationService
{
    private readonly INavigationService _navigationService;

    public AppNotificationService(INavigationService navigationService)
    {
        _navigationService = navigationService;
    }

    ~AppNotificationService()
    {
        Unregister();
    }

    public void Initialize()
    {
        AppNotificationManager.Default.NotificationInvoked += OnNotificationInvoked;

        AppNotificationManager.Default.Register();
    }

    public void OnNotificationInvoked(AppNotificationManager sender, AppNotificationActivatedEventArgs args)
    {

        App.MainWindow.DispatcherQueue.TryEnqueue(() =>
        {
            var welcomeMessage = "WelcomeNotice".GetLocalized().Replace("\\n", Environment.NewLine); ;
            App.MainWindow.ShowMessageDialogAsync(welcomeMessage, "WelcomeNoticeTitle".GetLocalized());

            App.MainWindow.BringToFront();
        });
    }

    public bool Show(string payload)
    {
        var appNotification = new AppNotification(payload);

        AppNotificationManager.Default.Show(appNotification);

        return appNotification.Id != 0;
    }

    public NameValueCollection ParseArguments(string arguments)
    {
        return HttpUtility.ParseQueryString(arguments);
    }

    public void Unregister()
    {
        AppNotificationManager.Default.Unregister();
    }
}

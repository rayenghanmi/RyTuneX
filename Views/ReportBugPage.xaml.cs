using Microsoft.UI;
using System.Diagnostics;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Text;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

using RyTuneX.ViewModels;
using RyTuneX.Services;
using RyTuneX.Notifications;
using Microsoft.Windows.AppNotifications;
using CommunityToolkit.WinUI.UI.Controls;
using RyTuneX.Contracts.Services;
using CommunityToolkit.WinUI;

namespace RyTuneX.Views;

public sealed partial class ReportBugPage : Page
{
    private static HttpClient client;
    public ReportBugViewModel ViewModel
    {
        get;
    }

    public ReportBugPage()
    {
        ViewModel = App.GetService<ReportBugViewModel>();
        InitializeComponent();
    }
    private static bool IsValidEmail(string email)
    {
        var emailPattern = @"^[a-zA-Z0-9._-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,4}$";
        return Regex.IsMatch(email, emailPattern);
    }

    private async void Webhook()
    {
        return;

        // Still Working on this

        /*var WebHookId = "";
        var WebHookToken = "";

        const string colorBlue = "3498db";
        var userEmail = "";
        var problemText = "";

        await Task.Run(() =>
        {
            DispatcherQueue.TryEnqueue(async () =>
            {
                try
                {
                    userEmail = email_holder.Text;
                    submit_holder.Document.GetText((Microsoft.UI.Text.TextGetOptions)Windows.UI.Text.TextGetOptions.AdjustCrlf, out problemText);
                    if (IsValidEmail(userEmail))
                    {
                        invalidEmail.Visibility = Visibility.Collapsed;
                        email_holder.BorderBrush = new SolidColorBrush(Colors.Transparent);
                        email_holder.Text = string.Empty;
                        submit_holder.Document.SetText(Microsoft.UI.Text.TextSetOptions.None, string.Empty);

                        var SuccessWebHook = new
                        {
                            username = System.Security.Principal.WindowsIdentity.GetCurrent().Name,
                            avatar_url = "https://i.imgur.com/QIIvClg.png",
                            embeds = new List<object>
                            {
                                new
                                {
                                    title = userEmail,
                                    url=$"https://mail.google.com/mail/?fs=1&to={userEmail}&su=Support&body&tf=cm",
                                    description = problemText,
                                    footer = new
                                    {
                                        text = $"build: {Assembly.GetExecutingAssembly().GetName().Version}",
                                    },
                                    timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                                    color = int.Parse(colorBlue, System.Globalization.NumberStyles.HexNumber)
                                }
                            }
                        };
                        try
                        {
                            var EndPoint = string.Format($"https://discordapp.com/api/webhooks/{WebHookId}/{WebHookToken}");

                            client ??= new HttpClient();

                            var content = new StringContent(JsonConvert.SerializeObject(SuccessWebHook), Encoding.UTF8, "application/json");

                            await client.PostAsync(EndPoint, content);
                            await App.MainWindow.ShowMessageDialogAsync("Your report was successfully sent, and we'll review it soon.",
                                "Report sent successfully");
                            Debug.WriteLine("Webhook successfully sent.");
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"Error sending webhook: {ex}");
                            await App.MainWindow.ShowMessageDialogAsync("We encountered an issue while attempting to send the crash report.\n" +
                                "Please check your network connection and try again. If the problem persists, please reach to the developer.",
                                "Error Sending Report");
                        }
                    }
                    else
                    {
                        await Task.Run(() =>
                        {
                            DispatcherQueue.TryEnqueue(() =>
                            {
                                invalidEmail.Visibility = Visibility.Visible;
                                email_holder.Text = string.Empty;
                                email_holder.BorderBrush = new SolidColorBrush((Windows.UI.Color)Application.Current.Resources["SystemErrorTextColor"]);
                            });
                        });
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error processing webhook: {ex}");
                }
            });
        });*/
    }

    private async void Send_discord(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        await Task.Run(() =>
        {
            Webhook();
        });
    }
}

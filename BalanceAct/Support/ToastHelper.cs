﻿using System;
using System.IO;
using System.Diagnostics;

using Microsoft.Windows.AppNotifications.Builder;
using Microsoft.Windows.AppNotifications;

using BalanceAct.Services;

using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;

namespace BalanceAct.Support;

public static class ToastHelper
{
    /// <summary>
    /// The <see cref="AppNotificationManager"/> appears to available on Windows 11 only.
    /// https://learn.microsoft.com/en-us/windows/apps/design/shell/tiles-and-notifications/adaptive-interactive-toasts?tabs=appsdk
    /// </summary>
    public static void ShowWarningToast(string header, string body, string buttonNotification)
    {
        if (AppNotificationManager.IsSupported())
        {
            var toastContent = new AppNotificationBuilder()
                    .AddText(header)
                    .AddText(body)
                    .SetAppLogoOverride(new Uri("ms-appx:///Assets/Warning.png"))
                    .AddButton(new AppNotificationButton(buttonNotification)
                        .SetInvokeUri(new Uri("https://github.com/GuildOfCalamity?tab=repositories")))
                    .BuildNotification();
            AppNotificationManager.Default.Show(toastContent);
        }
        else
        {
            ToastImageAndText(header, body, "Warning.png");
        }
    }

    /// <summary>
    /// The <see cref="AppNotificationManager"/> appears to available on Windows 11 only.
    /// https://learn.microsoft.com/en-us/windows/apps/design/shell/tiles-and-notifications/adaptive-interactive-toasts?tabs=appsdk
    /// </summary>
    public static void ShowStandardToast(string header, string body)
    {
        if (AppNotificationManager.IsSupported())
        {
            var toastContent = new AppNotificationBuilder()
            .AddText(header)
            .AddText(body)
            .SetAppLogoOverride(new Uri("ms-appx:///Assets/Balance.png"))
            .SetAttributionText("AboutAppName")
            .BuildNotification();
            AppNotificationManager.Default.Show(toastContent);
        }
        else
        {
            ToastImageAndText(header, body, "Balance.png");
        }
    }


    #region [Toast Routines]
    /// <summary>
    /// https://learn.microsoft.com/en-us/windows/apps/design/shell/tiles-and-notifications/toast-progress-bar?tabs=builder-syntax#using-data-binding-to-update-a-toast
    /// </summary>
    public static void ShowUpdatableToastWithProgress()
    {
        // Define a tag (and optionally a group) to uniquely identify the notification, in order update the notification data later;
        string tag = "weekly-playlist";
        string group = "downloads";

        XmlDocument toastXml = ToastNotificationManager.GetTemplateContent(ToastTemplateType.ToastImageAndText02);
        XmlNodeList stringElements = toastXml.GetElementsByTagName("text");

        // Generate the toast notification
        var toast = new ToastNotification(toastXml);

        // Assign the tag and group
        toast.Tag = tag;
        toast.Group = group;

        // Assign initial NotificationData values
        // Values must be of type string
        toast.Data = new NotificationData();
        toast.Data.Values["progressValue"] = "0.6";
        toast.Data.Values["progressValueString"] = "15/26 songs";
        toast.Data.Values["progressStatus"] = "Downloading...";

        // Provide sequence number to prevent out-of-order updates, or assign 0 to indicate "always update"
        toast.Data.SequenceNumber = 1;

        // Show the toast notification to the user
        ToastNotificationManager.CreateToastNotifier(App.GetCurrentNamespace()).Show(toast);
    }

    public static void UpdateProgress()
    {
        // Construct a NotificationData object;
        string tag = "weekly-playlist";
        string group = "downloads";

        // Create NotificationData and make sure the sequence number is incremented
        // since last update, or assign 0 for updating regardless of order
        var data = new NotificationData
        {
            SequenceNumber = 2
        };

        // Assign new values
        // Note that you only need to assign values that changed. In this example
        // we don't assign progressStatus since we don't need to change it
        data.Values["progressValue"] = "0.7";
        data.Values["progressValueString"] = "18/26 songs";

        // Update the existing notification's data by using tag/group
        var result = ToastNotificationManager.CreateToastNotifier(App.GetCurrentNamespace()).Update(data, tag, group);
        Debug.WriteLine($"[INFO] NotificationUpdateResult: {result}");
    }
    /// <summary>
    /// Notify user using a <see cref="ToastNotification"/>.
    /// Supports up to two lines of text and embeds the application image.
    /// </summary>
    static void ToastImageAndText(string header, string body = "", string asset = "Balance.png")
    {
        try
        {
            /*
                [ToastImageAndText01] Summary:
                     A large image and a single string wrapped across three lines of text. 
                     <toast><visual><binding template="ToastImageAndText01"><image id="1" src=""/><text id="1"></text></binding></visual></toast>

                [ToastImageAndText02] Summary:
                     A large image, one string of bold text on the first line, one string of regular
                     text wrapped across the second and third lines.
                     <toast><visual><binding template="ToastImageAndText02"><image id="1" src=""/><text id="1"></text><text id="2"></text></binding></visual></toast>
                
                [ToastImageAndText03] Summary:
                     A large image, one string of bold text wrapped across the first two lines, one
                     string of regular text on the third line. 
                     <toast><visual><binding template="ToastImageAndText03"><image id="1" src=""/><text id="1"></text><text id="2"></text></binding></visual></toast>
                
                [ToastImageAndText04] Summary:
                     A large image, one string of bold text on the first line, one string of regular
                     text on the second line, one string of regular text on the third line. 
                     <toast><visual><binding template="ToastImageAndText04"><image id="1" src=""/><text id="1"></text><text id="2"></text><text id="3"></text></binding></visual></toast>
                
                [ToastText01] Summary:
                     A single string wrapped across three lines of text. 
                     <toast><visual><binding template="ToastText01"><text id="1"></text></binding></visual></toast>

                [ToastText02] Summary:
                     One string of bold text on the first line, one string of regular text wrapped
                     across the second and third lines. 
                     <toast><visual><binding template="ToastText02"><text id="1"></text><text id="2"></text></binding></visual></toast>
                
                [ToastText03] Summary:
                     One string of bold text wrapped across the first and second lines, one string
                     of regular text on the third line. 
                     <toast><visual><binding template="ToastText03"><text id="1"></text><text id="2"></text></binding></visual></toast>
                
                [ToastText04] Summary:
                     One string of bold text on the first line, one string of regular text on the
                     second line, one string of regular text on the third line. 
                     <toast><visual><binding template="ToastText04"><text id="1"></text><text id="2"></text><text id="3"></text></binding></visual></toast>
            */

            XmlDocument toastXml = ToastNotificationManager.GetTemplateContent(ToastTemplateType.ToastImageAndText02);

            XmlNodeList stringElements = toastXml.GetElementsByTagName("text");

            stringElements.Item(0).AppendChild(toastXml.CreateTextNode(header));
            if (!string.IsNullOrEmpty(body))
                stringElements.Item(1).AppendChild(toastXml.CreateTextNode(body));

            #region [Set image tag]
            var imgElement = toastXml.GetElementsByTagName("image");
            // Default <image> properties
            // ImageElement[x].Attribute[0] = id
            // ImageElement[x].Attribute[1] = src
            if (App.IsPackaged)
                imgElement[0].Attributes[1].NodeValue = $"ms-appdata:///local/Assets/{asset}";
            else
                imgElement[0].Attributes[1].NodeValue = $"file:///{Directory.GetCurrentDirectory().Replace("\\", "/")}/Assets/{asset}";
            /*
              [NOTES]
              - "ms-appx:///Assets/Icon.png" 
                    Does not seem to work (I've tried setting the asset to Content and Resource with no luck).
                    This may work with the AppNotificationBuilder, but Win10 does not support AppNotificationBuilder.
              - "file:///D:/Icon.png" 
                    Does work, however it is read at runtime so if the asset is missing it will only show the text notification.
              - "ms-appdata:///local/Assets/Icon.png"
                    Would be for packaged apps, I have not tested this.
              - "https://static.cdn.com/media/someImage.png"
                    I have not tested this extensively. Early tests did not work.
            */
            #endregion

            ToastNotification toast = new ToastNotification(toastXml);

            toast.Activated += ToastOnActivated;
            toast.Dismissed += ToastOnDismissed;
            toast.Failed += ToastOnFailed;

            // NOTE: It is critical that you provide the applicationID during CreateToastNotifier().
            // It is the name that will be used in the action center to group your toasts.
            var tnm = ToastNotificationManager.CreateToastNotifier(App.GetCurrentNamespace());
            if (tnm == null)
            {
                Debug.WriteLine($"[WARNING] Could not create ToastNotificationManager.");
                return;
            }

            // This was not a problem during early testing but started to throw a COM Exception later on.
            //var canShow = tnm.Setting;
            //if (canShow != NotificationSetting.Enabled)
            //    Debug.WriteLine($"[WARNING] Not allowed to show notifications because '{canShow}'.");
            //else
                tnm.Show(toast);

        }
        catch (Exception ex)
        {
            Debug.WriteLine($"{ex.Message}, Method=ToastImageAndText");
        }
    }

    /// <summary>
    /// [Managing toast notifications in action center]
    /// https://learn.microsoft.com/en-us/previous-versions/windows/apps/dn631260(v=win.10)
    /// [The toast template catalog]
    /// https://learn.microsoft.com/en-us/previous-versions/windows/apps/hh761494(v=win.10)
    /// [Send a local toast notification from a C# app]
    /// https://learn.microsoft.com/en-us/windows/apps/design/shell/tiles-and-notifications/send-local-toast?tabs=desktop
    /// </summary>
    static void ToastOnActivated(ToastNotification sender, object args)
    {
        // Handle notification activation
        if (args is Windows.UI.Notifications.ToastActivatedEventArgs toastActivationArgs)
        {
            // Obtain the arguments from the notification
            Debug.WriteLine($"[INFO] ActivationArgs: {toastActivationArgs.Arguments}");

            if (App.WindowsVersion.Major >= 10 && App.WindowsVersion.Build >= 18362)
            {
                // Obtain any user input (text boxes, menu selections) from the notification
                Windows.Foundation.Collections.ValueSet userInput = toastActivationArgs.UserInput;

                // Do something with the user selection.
                foreach (var item in userInput)
                {
                    Debug.WriteLine($"ToastKey: '{item.Key}'  ToastValue: '{item.Value}'");
                }
            }
        }
    }
    static void ToastOnDismissed(ToastNotification sender, ToastDismissedEventArgs args)
    {
        Debug.WriteLine($"[INFO] Toast Dismissal Reason: {args.Reason}, Method=ToastOnDismissed");
    }
    static void ToastOnFailed(ToastNotification sender, ToastFailedEventArgs args)
    {
        Debug.WriteLine($"[ERROR] Toast Failed: {args.ErrorCode.Message}, Method=ToastOnFailed");
    }

    /// <summary>
    /// Shows use of the <see cref="ToastNotificationManager.History"/> feature.
    /// </summary>
    static void DumpToastHistory()
    {
        try
        {
            var notes = ToastNotificationManager.History.GetHistory(App.GetCurrentNamespace());
            foreach (var item in notes)
            {
                // Sample some of the fields.
                Debug.WriteLine($"[INFO] Notification expires: {item?.ExpirationTime}", LogLevel.Info);
                Debug.WriteLine($"[INFO] ExpiresOnReboot: {item?.ExpiresOnReboot}", LogLevel.Info);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ERROR] ShowToastHistory: {ex.Message}");
        }
    }

    public static void TestingStandardTemplates()
    {
        XmlDocument tt1 = ToastNotificationManager.GetTemplateContent(ToastTemplateType.ToastText01);
        Debug.WriteLine($"{tt1.GetXml()}");
        XmlDocument tiat1 = ToastNotificationManager.GetTemplateContent(ToastTemplateType.ToastImageAndText01);
        Debug.WriteLine($"{tiat1.GetXml()}");

        var toast = new ToastNotification(tiat1);
        ToastNotificationManager.CreateToastNotifier(App.GetCurrentNamespace()).Show(toast);
    }
    #endregion
}

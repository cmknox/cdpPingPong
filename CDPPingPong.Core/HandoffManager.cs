using System.Diagnostics;
using System.Linq;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Background;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;
using Newtonsoft.Json.Linq;

namespace CDPPingPong.Core
{
    public class HandoffManager
    {
        public enum RegisterTaskStatus { Succeeded, Failed, AlreadyRegistered }
        public static void Register()
        {
            BackgroundTaskHelper.RegisterSystemTriggerTask(SystemTriggerType.UserPresent, "User Present Task");
            BackgroundTaskHelper.RegisterSystemTriggerTask(SystemTriggerType.UserAway, "User Away Task");
            BackgroundTaskHelper.RegisterSystemTriggerTask(SystemTriggerType.TimeZoneChange, "Time Zone Task");
        }

        public static void OnUserPresent()
        {
            Debug.WriteLine("OnUserPresent");
            ActivityFeed.Start();

            var lastActivity = ActivityFeed.Activities.Where(a => a.Type == 1).OrderByDescending(o => o.LastModified).FirstOrDefault();

            // Show Toast
            if (lastActivity != null)
            {
                dynamic payload = JArray.Parse(lastActivity.Payload);
                string artifactType = payload.ArtifactTYpe;
                string sourceAppName = payload.SourceAppName;
                var activationId = string.Empty;
                switch (artifactType)
                {
                    case "App":
                        activationId = payload.SourceAppId;
                        break;
                    case "WebPage":
                        activationId = payload.Uri;
                        break;
                    case "AppItem":
                        activationId = payload.Uri;
                        break;
                }

                ShowToast(sourceAppName, activationId);
            }
        }

        public static void OnUserAway()
        {
            Debug.WriteLine("OnUserAway");

            ActivityFeed.Start();

            var lastActivity = ActivityFeed.Activities.Where(a => a.Type == 4).OrderByDescending(o => o.LastModified).FirstOrDefault();

            //publish last Activity as type 2
            var newActivity = new Activity
            {
                PackageId = Package.Current.Id.FamilyName,
                DeviceId = "0018BFFECAB43303",
                Type = 1,
                Priority = 1,
                Payload = lastActivity.Payload
            };
            //"0018BFFEC3585513";//"00187FFFBE535D3E";

            ActivityFeed.Add(newActivity);
        }

        private static void ShowToast(string text1, string text2)
        {
            XmlDocument toastXml = ToastNotificationManager.GetTemplateContent(ToastTemplateType.ToastText02);

            XmlNodeList stringElements = toastXml.GetElementsByTagName("text");

            var xmlNode = stringElements.Item(0);
            xmlNode?.AppendChild(toastXml.CreateTextNode(text1));

            var item = stringElements.Item(1);
            item?.AppendChild(toastXml.CreateTextNode(text2));

            ToastNotification notification = new ToastNotification(toastXml);
            notification.SuppressPopup = true;
            ToastNotificationManager.CreateToastNotifier().Show(notification);
        }
    }
}

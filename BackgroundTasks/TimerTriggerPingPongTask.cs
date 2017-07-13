using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Windows.Data.Xml.Dom;
using Windows.Storage;
using Windows.System.RemoteSystems;
using Windows.UI.Notifications;
using CDPPingPong.Core;
using Windows.Devices.Sensors;
using Windows.System.Threading;

namespace BackgroundTasks
{
    public sealed class TimerTriggerPingPongTask : IBackgroundTask
    {
        private const string MediaBackgroundTaskName = "Long Running Background Task";
        BackgroundTaskDeferral _taskDeferral;
        int _foundProximalDevices = 0;
        int _foundCloudDevices = 0;

        public void Run(IBackgroundTaskInstance taskInstance)
        {
            //Take a task deferral so the task isn't terminated
            _taskDeferral = taskInstance.GetDeferral();

            Events.Init();
            try
            {
                ShowToast("Roman Test Started", taskInstance.Task.Name);
                Events.TrackEvent(Events.BackgroundTaskStarted);
                Events.TrackPageView(taskInstance.Task.Name);

                PingPong.OnRemoteDeviceAdded += PingPong_OnRemoteDeviceAdded;
                PingPong.OnPongReceived += PingPong_OnPongReceived;
                PingPong.OnStatusUpdateMessage += PingPong_OnStatusUpdateMessage;

                RunTest();

                //if we're a long running task, set up our own timer
                if (taskInstance.Task.Name == MediaBackgroundTaskName)
                {
                    while (true)
                    {
                        Task.Delay(TimeSpan.FromMinutes(30)).Wait();
                        RunTest();           
                    }
                }
           
                taskInstance.Canceled += OnTaskCanceled;

                Events.TrackEvent(Events.BackgroundTaskExitedClean);
                ApplicationData.Current.LocalSettings.Values[taskInstance.Task.TaskId.ToString()] = "Success: " + DateTime.Now;
                _taskDeferral.Complete();
            }
            catch (Exception e)
            {
                Events.TrackException(e);
                throw;
            }
        }

        private void RunTest()
        {
            _foundProximalDevices = 0;
            _foundCloudDevices = 0;

            PingPong.DiscoverDevices();

            //wait around for responses
            Task.Delay(20000).Wait();

            ShowToast("Roman Test Finished", String.Format("Proximal: {0} Cloud: {1}", _foundProximalDevices, _foundCloudDevices));
        }

        private void PingPong_OnStatusUpdateMessage(object sender, string e) {}

        private void PingPong_OnPongReceived(object sender, PingPongMessage e) {}

        private void PingPong_OnRemoteDeviceAdded(object sender, RemoteSystem e)
        {
            if (e.IsAvailableByProximity)
            {
                _foundProximalDevices++;
            }
            else
            {
                _foundCloudDevices++;
            }
        }

        private void OnTaskCanceled(IBackgroundTaskInstance sender, BackgroundTaskCancellationReason reason)
        {
            if (_taskDeferral != null)
            {
                ApplicationData.Current.LocalSettings.Values[sender.Task.TaskId.ToString()] = "Cancelled: " + DateTime.Now;

                // Complete the service deferral
                _taskDeferral.Complete();
                _taskDeferral = null;
            }
        }

        public static void ShowToast(string text1, string text2)
        {
            XmlDocument toastXml = ToastNotificationManager.GetTemplateContent(ToastTemplateType.ToastText02);

            XmlNodeList stringElements = toastXml.GetElementsByTagName("text");

            stringElements.Item(0).AppendChild(toastXml.CreateTextNode(text1));

            stringElements.Item(1).AppendChild(toastXml.CreateTextNode(text2));

            ToastNotification notification = new ToastNotification(toastXml);
            notification.SuppressPopup = true;
            ToastNotificationManager.CreateToastNotifier().Show(notification);
        }

        public static async void RegisterTask()
        {
            var result = await BackgroundExecutionManager.RequestAccessAsync();

            RegisterTask("Timezone Background Task",
                typeof(TimerTriggerPingPongTask).FullName,
                new SystemTrigger(SystemTriggerType.TimeZoneChange, false));

            RegisterTask("InternetAvailable Background Task",
                typeof(TimerTriggerPingPongTask).FullName,
                new SystemTrigger(SystemTriggerType.InternetAvailable, false));

            RegisterTask("SessionConnected Background Task",
                typeof(TimerTriggerPingPongTask).FullName,
                new SystemTrigger(SystemTriggerType.SessionConnected, false));

            RegisterTask("UserPresent Background Task",
                typeof(TimerTriggerPingPongTask).FullName,
                new SystemTrigger(SystemTriggerType.UserPresent, false));
        }

        public static void RegisterTask(string taskName, string taskEntryPoint, IBackgroundTrigger trigger)
        {
            var existingTaskRegistrations = BackgroundTaskRegistration.AllTasks.Where(t => t.Value.Name == taskName);
            if (existingTaskRegistrations.Count() == 0)
            {
                foreach (var item in BackgroundTaskRegistration.AllTasks)
                {
                    var task = item.Value;
                    if (task.Name == taskName)
                    {
                        task.Unregister(true);
                    }
                }

                try
                {
                    // Create a new background task builder.
                    var taskBuilder = new BackgroundTaskBuilder();

                    // Associate the SmsReceived trigger with the background task builder.
                    taskBuilder.SetTrigger(trigger);

                    // Specify the background task to run when the trigger fires.
                    taskBuilder.TaskEntryPoint = taskEntryPoint;

                    // Name the background task.
                    taskBuilder.Name = taskName;

                    // Register the background task.
                    var taskRegistration = taskBuilder.Register();

                    Debug.WriteLine(taskName + " registered " + taskRegistration.TaskId);

                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }
            }
            else
            {
                foreach (var taskRegistration in existingTaskRegistrations)
                {
                    var task = taskRegistration.Value;
                    var settings = Windows.Storage.ApplicationData.Current.LocalSettings;
                    var status = settings.Values[task.TaskId.ToString()];
                    Debug.WriteLine(taskName + " last status" + ": " + status);
                }
            }
        }
    }
}

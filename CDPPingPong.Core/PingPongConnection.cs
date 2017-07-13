using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.AppService;
using Windows.ApplicationModel.Background;

namespace CDPPingPong.Core
{
    public sealed class PingPongConnection
    {
        BackgroundTaskDeferral serviceDeferral;
        AppServiceConnection connection;

        public PingPongConnection(IBackgroundTaskInstance taskInstance)
        {
            // Take a service deferral so the service isn't terminated
            serviceDeferral = taskInstance.GetDeferral();

            taskInstance.Canceled += OnTaskCanceled;

            var details = taskInstance.TriggerDetails as AppServiceTriggerDetails;
            connection = details.AppServiceConnection;

            // Listen for incoming app service requests
            connection.RequestReceived += OnRequestReceived;

            PingPong.NotifyAppServiceConnected(connection);
        }

        private void OnTaskCanceled(IBackgroundTaskInstance sender, BackgroundTaskCancellationReason reason)
        {
            if (serviceDeferral != null)
            {
                // Complete the service deferral
                serviceDeferral.Complete();
                serviceDeferral = null;
            }
        }

        public static async void OnRequestReceived(AppServiceConnection sender, AppServiceRequestReceivedEventArgs args)
        {
            // Get a deferral so we can use an awaitable API to respond to the message
            var messageDeferral = args.GetDeferral();

            try
            {
                var result = await PingPong.HandleCommand(args.Request.Message);
                await args.Request.SendResponseAsync(result);
            }
            finally
            {
                // Complete the message deferral so the platform knows we're done responding
                messageDeferral.Complete();
            }
        }
    }
}

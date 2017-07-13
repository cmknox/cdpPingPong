using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.System;
using Windows.UI.Xaml;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;

namespace CDPPingPong.Core
{
    public class Events
    {
        public const string CompletedPingEvent = "CompletedPingEvent";
        public const string AppLaunched = "AppLaunched";
        public const string AppExited = "AppExited";
        public const string BackgroundTaskStarted = "BackgroundTaskStarted";
        public const string BackgroundTaskExitedClean = "BackgroundTaskExited.Clean";
        public const string StartRemoteSystemWatcher = "StartRemoteSystemWatcher";
        public const string RemoteSystemUpdated = "RemoteSystemUpdated";
        public const string RemoteSystemRemoved = "RemoteSystemRemoved";
        public const string RemoteSystemAdded = "RemoteSystemAdded";

        public const string LaunchUriAsyncSuccess = "LaunchUriAsync.Success";
        public const string LaunchUriAsyncStart = "LaunchUriAsyncStart";
        public const string LaunchUriAsyncErrorDeniedByRemoteSystem = "LaunchUriAsyncErrorDeniedByRemoteSystem";
        public const string LaunchUriAsyncErrorProtocolUnavailable = "LaunchUriAsyncErrorProtocolUnavailable";
        public const string LaunchUriAsyncErrorRemoteSystemUnavailable = "LaunchUriAsyncErrorRemoteSystemUnavailable";
        public const string LaunchUriAsyncErrorOther = "LaunchUriAsyncErrorOther";

        public static TelemetryClient _telemetryClient;
        public static bool isInitialized = false;

        public static void Init(Windows.UI.Xaml.Application app)
        {
            app.UnhandledException += CurrentDomain_UnhandledException; 
            Init();
        }

        public static void Init()
        {
            // Initialize HockeyApp
            Microsoft.HockeyApp.HockeyClient.Current.Configure("ab9bf227424c433c923b0cb529cf0928");

            // Initialize App Insights
            string instrumentationKey = "5312af02-2009-4b63-b77b-1e8443bce62d";

            if (Debugger.IsAttached)
            {
                instrumentationKey = "2ae207ad-46d2-4fda-817d-87e057f6725b"; //test server
            }

            Microsoft.ApplicationInsights.WindowsAppInitializer.InitializeAsync(
                    instrumentationKey,
                    WindowsCollectors.Metadata |
                    WindowsCollectors.Session);

            _telemetryClient = new TelemetryClient() {InstrumentationKey = instrumentationKey };
            _telemetryClient.Context.Session.Id = Guid.NewGuid().ToString();
            SetUserId();


            isInitialized = true;
        }

        private static async void SetUserId()
        {
            var users = await Windows.System.User.FindAllAsync();
            string id = users.First()?.NonRoamableId;
            _telemetryClient.Context.User.AccountId = id;
            _telemetryClient.Context.User.Id = id;
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            TrackException((Exception)e.Exception);
        }

        public static void TrackException(Exception e)
        {
            if (isInitialized == true)
            {
                var excTelemetry = new ExceptionTelemetry(e)
                {
                    SeverityLevel = SeverityLevel.Critical,
                    HandledAt = ExceptionHandledAt.Unhandled
                };

                _telemetryClient.TrackException(excTelemetry);

                _telemetryClient.Flush();
            }
        }

        public static void TrackEvent(string eventName)
        {
            if (isInitialized == true)
            {
                var telemetryContext = new TelemetryContext();

                _telemetryClient.TrackEvent(eventName);
                Microsoft.HockeyApp.HockeyClient.Current.TrackEvent(eventName);
            }
        }

        public static void TrackPageView(string pageName)
        {
            _telemetryClient.TrackPageView(pageName);
        }

        internal static void TrackEvent(string eventName, TimeSpan roundTripTime)
        {
            var measurements = new Dictionary<string, double> {{"Time", roundTripTime.TotalMilliseconds}};
            _telemetryClient.TrackEvent(eventName, null, measurements);
        }
    }
}

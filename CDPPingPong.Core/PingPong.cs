using System;
using System.Collections.Generic;
using System.Diagnostics;
using Windows.ApplicationModel;
using Windows.ApplicationModel.AppService;
using Windows.Foundation.Collections;
using Windows.System.RemoteSystems;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;

namespace CDPPingPong.Core
{
    public sealed class PingPong
    {
        private static RemoteSystemWatcher _watcher;
        private static readonly List<RemoteSystem> RemoteSystemList = new List<RemoteSystem>();

        public static event EventHandler<AppServiceConnection> OnAppServiceConnected;
        public static event EventHandler<RemoteSystem> OnRemoteDeviceAdded;
        public static event EventHandler<RemoteSystem> OnRemoteDeviceUpdated;
        public static event EventHandler<string> OnRemoteDeviceRemoved;

        public static event EventHandler<PingPongMessage> OnPongReceived;
        public static event EventHandler<string> OnPongReceivedMessage;
        public static event EventHandler<string> OnStatusUpdateMessage;
        public static event EventHandler<string> OnErrorMessage;

        private static async void RequestAccess()
        {
            try
            {
                await RemoteSystem.RequestAccessAsync();
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
        }

        public static RemoteSystemDiscoveryTypeFilter DiscoveryFilter = new RemoteSystemDiscoveryTypeFilter(RemoteSystemDiscoveryType.Any);
        public static RemoteSystemAuthorizationKindFilter AutorizationFilter = new RemoteSystemAuthorizationKindFilter(RemoteSystemAuthorizationKind.SameUser);
        public static RemoteSystemKindFilter KindFilter = null;

        private static List<IRemoteSystemFilter> BuildFilters()
        {
            var filters = new List<IRemoteSystemFilter> {DiscoveryFilter};

            if (KindFilter != null)
            {
                filters.Add(KindFilter);
            }

            var statusFilter = new RemoteSystemStatusTypeFilter(RemoteSystemStatusType.Any);
            filters.Add(statusFilter);

            var kindFilterString = KindFilter != null ? string.Join(",", KindFilter.RemoteSystemKinds.ToArray()) : string.Empty;
            DebugWrite($"RebuiltFilter: {DiscoveryFilter.RemoteSystemDiscoveryType} {statusFilter.RemoteSystemStatusType} {kindFilterString}");
            return filters;
        }

        public static void DiscoverDevices()
        {
            RequestAccess();

            // Create Watcher for remote devices
            _watcher = RemoteSystem.CreateWatcher(BuildFilters());

            // Hook up event handlers
            _watcher.RemoteSystemAdded += _watcher_RemoteSystemAdded;
            _watcher.RemoteSystemRemoved += _watcher_RemoteSystemRemoved;
            _watcher.RemoteSystemUpdated += _watcher_RemoteSystemUpdated;

            // Start the Watcher
            _watcher.Start();
            Events.TrackEvent(Events.StartRemoteSystemWatcher);
        }

        private static void _watcher_RemoteSystemUpdated(RemoteSystemWatcher sender, RemoteSystemUpdatedEventArgs args)
        {
            // Remote and re-add remote system
            var remoteSystem = args.RemoteSystem;
            RemoteSystemList.RemoveAll(d => d.Id == remoteSystem.Id);
            RemoteSystemList.Add(remoteSystem);

            Events.TrackEvent(Events.RemoteSystemUpdated);

            DebugWrite(
                $"RemoteSystemUpdated: {remoteSystem.DisplayName} {remoteSystem.Id} {remoteSystem.IsAvailableByProximity} {remoteSystem.Kind} {remoteSystem.Status}");

            OnRemoteDeviceUpdated?.Invoke(null, remoteSystem);
        }

        private static void _watcher_RemoteSystemRemoved(RemoteSystemWatcher sender, RemoteSystemRemovedEventArgs args)
        {
            // Remove all remote systems with matching Ids
            RemoteSystemList.RemoveAll(d => d.Id == args.RemoteSystemId);
            Events.TrackEvent(Events.RemoteSystemRemoved);

            DebugWrite("RemoteSystemRemove: " + args.RemoteSystemId);
            OnRemoteDeviceRemoved?.Invoke(null, args.RemoteSystemId);
        }

        private static void _watcher_RemoteSystemAdded(RemoteSystemWatcher sender, RemoteSystemAddedEventArgs args)
        {
            var remoteSystem = args.RemoteSystem;
            Events.TrackEvent(Events.RemoteSystemAdded);

            RemoteSystemList.Add(remoteSystem);
            DebugWrite(
                $"RemoteSystemAdded: {remoteSystem.DisplayName} {remoteSystem.Id} {remoteSystem.IsAvailableByProximity} {remoteSystem.Kind} {remoteSystem.Status}");

            OnRemoteDeviceAdded?.Invoke(null, remoteSystem);
        }

        public static async void CreateAppService(RemoteSystem remoteSystem)
        {
            AppServiceConnection connection = new AppServiceConnection();
            connection.AppServiceName = "com.microsoft.test.cdppingpongservice";
            connection.PackageFamilyName = Package.Current.Id.FamilyName;
            
            RemoteSystemConnectionRequest connectionRequest = new RemoteSystemConnectionRequest(remoteSystem);
            var status = await connection.OpenRemoteAsync(connectionRequest);
            if (SuccessfulAppServiceClientConnectionStatus(status, connection, remoteSystem) == true)
            {
                connection.RequestReceived += PingPongConnection.OnRequestReceived;

                NotifyAppServiceConnected(connection);
            }
            else
            {
                DebugWrite($"Failed to create app service connection to ({remoteSystem.DisplayName}, {remoteSystem.Id})");
            }
        }

        public static void SendPingPongMessage(AppServiceConnection appService)
        {
            SendMessageInternal(appService, PingPongMessage.CreatePingCommand(appService).ToValueSet());
        }

        private static async void SendMessageInternal(AppServiceConnection connection, ValueSet valueSet)
        {
            AppServiceResponse response = await connection.SendMessageAsync(valueSet);

            if (response.Status == AppServiceResponseStatus.Success)
            {
                var pongMessage = PingPongMessage.FromValueSet(response.Message);
                OnPongReceived?.Invoke(null, pongMessage);

                var roundTripTime = (DateTime.Now - pongMessage.CreationDate);
                OnPongReceivedMessage?.Invoke(null, "Pong received from [" + pongMessage.TargetId + "] with RTT [" + roundTripTime + "]");
            }
            else
            {

                OnPongReceivedMessage?.Invoke(null, "Pong received was a failure with status [" + response.Status.ToString() + "]");
            }
        }

        private static bool SuccessfulAppServiceClientConnectionStatus(AppServiceConnectionStatus status, AppServiceConnection connection, RemoteSystem remoteSystem)
        {
            switch (status)
            {
                case AppServiceConnectionStatus.AppServiceUnavailable:
                    DebugWrite(string.Format("The app AppServicesProvider is installed but it does not provide the app service {0}.", connection.AppServiceName));
                    return false;
                case AppServiceConnectionStatus.RemoteSystemUnavailable:
                    DebugWrite($"The remote system is unavailable ({remoteSystem.DisplayName}, {remoteSystem.Id}) ");
                    return false;
                case AppServiceConnectionStatus.RemoteSystemNotSupportedByApp:
                    DebugWrite($"The remote app does not support remote invocation ({remoteSystem.DisplayName}, {remoteSystem.Id})");
                    return false;
                case AppServiceConnectionStatus.AppNotInstalled:
                    DebugWrite($"The remote app is not installed ({remoteSystem.DisplayName}, {remoteSystem.Id})");
                    return false;
                case AppServiceConnectionStatus.AppUnavailable:
                    DebugWrite($"The remote app is not available ({remoteSystem.DisplayName}, {remoteSystem.Id})");
                    return false;
                case AppServiceConnectionStatus.Unknown:
                    DebugWrite($"Unknown AppServiceConnectionStatus ({remoteSystem.DisplayName}, {remoteSystem.Id})");
                    return false;
                case AppServiceConnectionStatus.NotAuthorized:
                    DebugWrite($"The remote app service connection is not authorized ({remoteSystem.DisplayName}, {remoteSystem.Id})");
                    return false;
                case AppServiceConnectionStatus.Success:
                    return true;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private static void DebugWrite(string v)
        {
            Debug.WriteLine(v);
            OnStatusUpdateMessage?.Invoke(null, v);
        }

        private static void ErrorWrite(string v)
        {
            Debug.WriteLine(v);
            OnErrorMessage?.Invoke(null, v);
        }

        public static async Task<ValueSet> HandleCommand(ValueSet message)
        {
            object typeObj;
            message.TryGetValue("Type", out typeObj);
            var type = typeObj as string;

            if (type == "ping")
            {
                return PingPongMessage.CreatePongCommand(PingPongMessage.FromValueSet(message)).ToValueSet();
            }
            else if (type == "CCSCommand")
            {
                return new ValueSet
                {
                    ["Type"] = "Ack",
                    ["CreationDate"] = DateTime.Now.ToString(CultureInfo.InvariantCulture),
                };
            }
            else if (type == "LaunchUri")
            {
                object uriObj;
                message.TryGetValue("Uri", out uriObj);
                var uriString = uriObj as string ?? "http:// bing.com";

                await LauncherHelper.LaunchUriAsync(new Uri(uriString));
                return new ValueSet
                {
                    ["Type"] = "Ok",
                    ["CreationDate"] = DateTime.Now.ToString(CultureInfo.InvariantCulture),
                };
            }
            else
            {
                return new ValueSet
                {
                    ["Type"] = "Ack", ["CreationDate"] = DateTime.Now.ToString(CultureInfo.InvariantCulture),
                };
            }
        }

        public static void NotifyAppServiceConnected(AppServiceConnection connection)
        {
            OnAppServiceConnected?.Invoke(null, connection);
        }
    }
}

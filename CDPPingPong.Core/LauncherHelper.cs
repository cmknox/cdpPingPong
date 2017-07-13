using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.System;
using Windows.System.RemoteSystems;

namespace CDPPingPong.Core
{
    public static class LauncherHelper
    {
        public static async Task LaunchUriAsync(Uri uri)
        {
            var result = await Launcher.LaunchUriAsync(uri);

        }
        public static async Task LaunchRemoteUriAsync(RemoteSystem remoteSystem, Uri uri)
        {
            if (remoteSystem != null)
            {
                var connection = new RemoteSystemConnectionRequest(remoteSystem);

                Events.TrackEvent(Events.LaunchUriAsyncStart);
                var result = await RemoteLauncher.LaunchUriAsync(connection, uri);

                if (result == RemoteLaunchUriStatus.Success)
                {
                    Events.TrackEvent(Events.LaunchUriAsyncSuccess);
                }
                switch (result)
                {
                    case RemoteLaunchUriStatus.Success:
                        Events.TrackEvent(Events.LaunchUriAsyncSuccess);
                        break;
                    case RemoteLaunchUriStatus.DeniedByRemoteSystem:
                        Events.TrackEvent(Events.LaunchUriAsyncErrorDeniedByRemoteSystem);
                        break;
                    case RemoteLaunchUriStatus.ProtocolUnavailable:
                        Events.TrackEvent(Events.LaunchUriAsyncErrorProtocolUnavailable);
                        break;
                    case RemoteLaunchUriStatus.RemoteSystemUnavailable:
                        Events.TrackEvent(Events.LaunchUriAsyncErrorRemoteSystemUnavailable);
                        break;
                    default:
                        Events.TrackEvent(Events.LaunchUriAsyncErrorOther);
                        break;
                }
            }
        }
    }
}

using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Windows.System.RemoteSystems;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using BackgroundTasks;
using CDPPingPong.Core;
using Windows.UI.Xaml.Media.Animation;
using Windows.ApplicationModel;
using Windows.UI.Popups;

namespace CDPPingPong
{
    public sealed class RemoteSystemInfo :INotifyPropertyChanged
    {
        private string _pingTime;
        private string _id;
        private RemoteSystem _remoteSystem;

        private static readonly Dictionary<string, string> RemoteSystemKindImages = new Dictionary<string, string>
        {
            { RemoteSystemKinds.Desktop, "ms-appx:///Assets/Devices/Desktop.png" },
            { RemoteSystemKinds.Phone, "ms-appx:///Assets/Devices/Phone.png" },
            { RemoteSystemKinds.Xbox, "ms-appx:///Assets/Devices/Xbox.png" },
            { RemoteSystemKinds.Holographic, "ms-appx:///Assets/Devices/Hololens.png" },
            { RemoteSystemKinds.Hub, "ms-appx:///Assets/Devices/SurfaceHub.png" },
            { "Unknown", "ms-appx:///Assets/Devices/Unknown.png"}
        };

        public RemoteSystemInfo(RemoteSystem remoteSystem)
        {
            this.RemoteSystem = remoteSystem;

            UpdateRemoteSystem();
        }

        private async void UpdateRemoteSystem()
        {
            Name = RemoteSystem.DisplayName;
            Id = RemoteSystem.Id;
            Proximal = RemoteSystem.IsAvailableByProximity ? "Proximal" : "Cloud";
            Kind = RemoteSystem.Kind.ToString();

            if (RemoteSystemKindImages.ContainsKey(RemoteSystem.Kind))
            {
                KindImage = RemoteSystemKindImages[RemoteSystem.Kind];
            }
            else
            {
                KindImage = RemoteSystemKindImages["Unknown"];
            }

            Status = RemoteSystem.Status.ToString();
            // Set System capability list
            try
            {
                bool supportsSpatialEntity = await RemoteSystem.GetCapabilitySupportedAsync(KnownRemoteSystemCapabilities.SpatialEntity);
                SupportsSpatial = supportsSpatialEntity.ToString();
                
                bool supportsAppService = await this.RemoteSystem.GetCapabilitySupportedAsync(KnownRemoteSystemCapabilities.AppService);
                SupportsAppServices = supportsAppService.ToString();
                
                bool supportsLaunchUri = await RemoteSystem.GetCapabilitySupportedAsync(KnownRemoteSystemCapabilities.LaunchUri);
                SupportsLauncher = supportsLaunchUri.ToString();
                
                bool supportsRemoteSession = await RemoteSystem.GetCapabilitySupportedAsync(KnownRemoteSystemCapabilities.RemoteSession);
                SupportsRemoteSession = supportsRemoteSession.ToString();
            }
            catch (Exception e)
            {
                // Handle exception
                string exceptionMessage = e.Message;
            }
        }

        public RemoteSystem RemoteSystem
        {
            get { return _remoteSystem; }
            set
            {
                _remoteSystem = value;

                UpdateRemoteSystem();

                OnPropertyChanged();
            }
        }

        public string Name { get; set; }
        public string Proximal { get; set; }
        public string Kind { get; set; }
        public string KindImage { get; set; }

        public string Status { get; set; }

        // Add known resource capability list here. TODO: convert these into a list
     
        public string SupportsSpatial { get; set; }
        public string SupportsAppServices { get; set; }
        public string SupportsLauncher { get; set; }
        public string SupportsRemoteSession { get; set; }

        public string Id
        {
            get { return _id; }
            set
            {
                if (value == _id) return;
                _id = value;
                OnPropertyChanged();
            }
        }

        public string PingTime
        {
            get { return _pingTime; }
            set
            {
                if (value == _pingTime) return;
                _pingTime = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
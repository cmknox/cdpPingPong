using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Windows.ApplicationModel.AppService;
using Windows.System.RemoteSystems;

namespace CDPPingPong
{
    public sealed class AppServiceInfo : INotifyPropertyChanged
    {
        private string _packageFamilyName;
        private string _appServiceName;

        private AppServiceConnection _connection;

        private static readonly Dictionary<string, string> RemoteSystemKindImages = new Dictionary<string, string>
        {
            { RemoteSystemKinds.Desktop, "ms-appx:///Assets/Devices/Desktop.png" },
            { RemoteSystemKinds.Phone, "ms-appx:///Assets/Devices/Phone.png" }
        };

        public AppServiceInfo(AppServiceConnection connection)
        {
            this.AppServiceConnection = connection;

            UpdateRemoteSystem();
        }

        private void UpdateRemoteSystem()
        {
            PackageFamilyName = AppServiceConnection.PackageFamilyName;
            AppServiceName = AppServiceConnection.AppServiceName;

            // Android devices do not give the PackageFamilyName, so we assume this is a Phone 
            if (PackageFamilyName == "")
            {
                KindImage = RemoteSystemKindImages[RemoteSystemKinds.Phone];
            }
            // Else, we use an image of a Desktop since that is the most likely device,
            // and we have no information to determine the actual kind of the connected device
            else
            {
                KindImage = RemoteSystemKindImages[RemoteSystemKinds.Desktop];
            }
        }

        public AppServiceConnection AppServiceConnection
        {
            get { return _connection; }
            set
            {
                _connection = value;

                UpdateRemoteSystem();

                OnPropertyChanged();
            }
        }
        
        public string PackageFamilyName
        {
            get { return _packageFamilyName; }
            set
            {
                if (value == _packageFamilyName) return;
                _packageFamilyName = value;
                OnPropertyChanged();
            }
        }

        public string AppServiceName
        {
            get { return _appServiceName; }
            set
            {
                if (value == _appServiceName) return;
                _appServiceName = value;
                OnPropertyChanged();
            }
        }

        public string KindImage { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
using CDPPingPong.Core;
using System;
using System.Collections.ObjectModel;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace CDPPingPong.Controls
{
    public sealed partial class RemoteSystemDetailsControl : UserControl
    {
        public RemoteSystemInfo SelectedSystem => DataContext as RemoteSystemInfo;

        public ObservableCollection<string> Suggestions = new ObservableCollection<string>(new string[]
        {
            "http://bing.com",
            "http://msn.com",
            "ms-windows-store://pdp/?productid=9NBLGGH4NNQJ",
            "spotify:artist:06HL4z0CvFAxyc27GXpf02"
        });

        public RemoteSystemDetailsControl()
        {
            this.InitializeComponent();
            UriSuggestTextBox.ItemsSource = Suggestions;

            PingPong.OnPongReceived += PingPong_OnPongReceived;

            this.DataContextChanged += RemoteSystemDetailsControl_DataContextChanged;
        }

        private async void PingPong_OnPongReceived(object sender, PingPongMessage e)
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                if (SelectedSystem.Id == e.TargetId)
                {
                    SelectedSystem.PingTime = e.RoundTripTime.TotalMilliseconds.ToString("0") + "ms";
                    Bindings.Update();
                }
            });
        }

        private void RemoteSystemDetailsControl_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            Bindings.Update();
        }

        private void System_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e) {}

        private async void LaunchUriButton_Click(object sender, RoutedEventArgs e)
        {
            var uri = new UriBuilder(UriSuggestTextBox.Text).Uri;
            var remoteSystem = SelectedSystem?.RemoteSystem;
            await LauncherHelper.LaunchRemoteUriAsync(remoteSystem, uri);
        }

        private void CreateAppServiceButton_Click(object sender, RoutedEventArgs e)
        {
            var remoteSystem = SelectedSystem?.RemoteSystem;

            if (remoteSystem != null)
            {
                PingPong.CreateAppService(remoteSystem);
            }
        }

        private void UriSuggestTextBox_OnGotFocus(object sender, RoutedEventArgs e)
        {
            UriSuggestTextBox.IsSuggestionListOpen = true;
        }
    }
}

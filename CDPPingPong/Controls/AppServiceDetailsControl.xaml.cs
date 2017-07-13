using System;
using System.Diagnostics;
using CDPPingPong.Core;
using System.Collections.ObjectModel;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;


// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace CDPPingPong.Controls
{
    public sealed partial class AppServiceDetailsControl : UserControl
    {
        public AppServiceInfo SelectedAppService => DataContext as AppServiceInfo;

        private ObservableCollection<string> StatusMessageCollection { get; set; } = new ObservableCollection<string>();

        public AppServiceDetailsControl()
        {
            this.InitializeComponent();
            this.DataContextChanged += AppServiceControl_DataContextChanged;

            PingPong.OnPongReceivedMessage += PingPong_OnPongReceived;

            StatusMessageListView.ItemsSource = StatusMessageCollection;
        }

        private void AppServiceControl_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args) {}

        private void System_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e) {}

        private void PingAppService_Button_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedAppService != null)
            {
                LogMessage("Sending Ping Message");
                PingPong.SendPingPongMessage(SelectedAppService.AppServiceConnection);
            }
            else
            {
                LogMessage("Error: No SelectedAppService selected");
            }
        }

        private void CloseAppService_Button_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Close App Service
            LogMessage("Not implemented yet");

            if (SelectedAppService != null)
            {
                LogMessage("Closing App Service");
            }
            else
            {
                LogMessage("Error: No SelectedAppService selected");
            }
        }

        private async void PingPong_OnPongReceived(object sender, string str)
        {
            LogMessage(str);
        }

        private async void LogMessage(string str)
        {
            Debug.WriteLine(str);
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                StatusMessageCollection.Add(str);
            });
        }
    }
}

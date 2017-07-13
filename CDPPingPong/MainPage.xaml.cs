using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using Windows.System;
using Windows.System.RemoteSystems;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using BackgroundTasks;
using CDPPingPong.Core;
using Windows.UI.Xaml.Media.Animation;
using Windows.ApplicationModel;
using Windows.UI.Popups;
using CDPPingPong.Controls;
using Windows.ApplicationModel.AppService;

// The Blank Page item template is documented at http:// go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace CDPPingPong
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage
    {
        private RemoteSystemInfo selectedSystem;
        private AppServiceInfo selectedAppService;
        private bool _registeredHandlers;

        private ObservableCollection<RemoteSystemInfo> RemoteSystemInfoCollection { get; set; } = new ObservableCollection<RemoteSystemInfo>();
        private ObservableCollection<AppServiceInfo> AppServiceInfoCollection { get; set; } = new ObservableCollection<AppServiceInfo>();

        public MainPage()
        {
            InitializeComponent();
            RemoteSystemListView.ItemsSource = RemoteSystemInfoCollection;
            AppServiceListView.ItemsSource = AppServiceInfoCollection;
            StatusMessageListView.ItemsSource = StatusMessageCollection;
            this.Loaded += MainPage_Loaded;
            TimerTriggerPingPongTask.RegisterTask();
        }

        private void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            // If the app starts in narrow mode - showing only the Master listView
            if (PageSizeStatesGroup.CurrentState == NarrowState)
            {
                VisualStateManager.GoToState(this, MasterState.Name, true);
            }
            else if (PageSizeStatesGroup.CurrentState == WideState)
            {
                // In this case, the app starts is wide mode, Master/Details view
                VisualStateManager.GoToState(this, MasterDetailsState.Name, true);
            }
        }

        private void RemoteSystemListView_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SelectSystem(RemoteSystemListView.SelectedItem as RemoteSystemInfo);
        }

        private void AppServiceListView_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SelectSystem(AppServiceListView.SelectedItem as AppServiceInfo);
        }

        private void RemoteSystemListView_OnItemClick(object sender, ItemClickEventArgs e)
        {
            // The clicked item it is the new selected contact
            selectedSystem = e.ClickedItem as RemoteSystemInfo;
            SelectSystem(selectedSystem);
        }

        private void AppServiceListView_OnItemClick(object sender, ItemClickEventArgs e)
        {
            selectedAppService = e.ClickedItem as AppServiceInfo;
            SelectSystem(selectedAppService);
        }

        private void SelectSystem(RemoteSystemInfo system)
        {
            selectedSystem = system;
            DetailContentPresenter.Visibility = Visibility.Visible;
            AppServiceDetailContentPresenter.Visibility = Visibility.Collapsed;

            if (PageSizeStatesGroup.CurrentState == NarrowState)
            {
                // Go to the details page and display the item 
                Frame.Navigate(typeof(DetailsPage), selectedSystem, new DrillInNavigationTransitionInfo());
            }
            else
            {
                // Play a refresh animation when the user switches detail items.
                EnableContentTransitions();
            }
        }

        private void SelectSystem(AppServiceInfo appService)
        {
            selectedAppService = appService;
            AppServiceDetailContentPresenter.Visibility = Visibility.Visible;
            DetailContentPresenter.Visibility = Visibility.Collapsed;

            if (PageSizeStatesGroup.CurrentState == NarrowState)
            {
                // Go to the details page and display the item 
                Frame.Navigate(typeof(AppServiceDetailsPage), selectedAppService, new DrillInNavigationTransitionInfo());
            }
            else
            {
                // Play a refresh animation when the user switches detail items.
                EnableContentTransitions();
            }
        }

        private void EnableContentTransitions()
        {
            DetailContentPresenter.ContentTransitions.Clear();
            DetailContentPresenter.ContentTransitions.Add(new EntranceThemeTransition());
        }

        private void PageSizeStatesGroup_OnCurrentStateChanged(object sender, VisualStateChangedEventArgs e)
        {
            bool isNarrow = e.NewState == NarrowState;
            if (isNarrow)
            {
                Frame.Navigate(typeof(DetailsPage), selectedSystem, new SuppressNavigationTransitionInfo());
            }
            else
            {
                VisualStateManager.GoToState(this, MasterDetailsState.Name, true);
                RemoteSystemListView.SelectedItem = selectedSystem;
            }

            EntranceNavigationTransitionInfo.SetIsTargetElement(RemoteSystemListView, isNarrow);
            if (DetailContentPresenter != null)
            {
                EntranceNavigationTransitionInfo.SetIsTargetElement(DetailContentPresenter, !isNarrow);
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            Events.TrackPageView(typeof(MainPage).FullName);

            if (RemoteSystemInfoCollection.Count == 0)
            {
                RefreshDevices();
            }

            HomeburgerPane.SamplesSplitView.IsPaneOpen = false;
        }

        private ObservableCollection<string> StatusMessageCollection { get; set; } = new ObservableCollection<string>();

        private async void PingPong_OnStatusUpdateMessage(object sender, string e)
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                StatusMessageCollection.Add(e);
            });
        }

        private async void PingPong_OnErrorMessage(object sender, string e)
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                ErrorTextBlock.Text = e;
            });
        }

        private async void PingPong_OnRemoteDeviceAdded(object sender, RemoteSystem e)
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                var system = new RemoteSystemInfo(e);
                RemoteSystemInfoCollection.Add(system);
            });
        }

        private async void PingPong_OnAppServiceConnected(object sender, AppServiceConnection e)
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                var appService = new AppServiceInfo(e);
                AppServiceInfoCollection.Add(appService);
            });
        }

        private async void PingPong_OnRemoteDeviceRemoved(object sender, string e)
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                var remoteSystemInfo = RemoteSystemInfoCollection.FirstOrDefault(d => d.Id == e);
                if (remoteSystemInfo != null)
                {
                    RemoteSystemInfoCollection.Remove(remoteSystemInfo);
                }
            });
        }

        private async void PingPong_OnRemoteDeviceUpdated(object sender, RemoteSystem e)
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                var remoteSystemInfo = RemoteSystemInfoCollection.FirstOrDefault(d => d.Id == e.Id);
                if (remoteSystemInfo != null)
                {
                    
                    int index = RemoteSystemInfoCollection.IndexOf(remoteSystemInfo);
                    RemoteSystemInfoCollection.RemoveAt(index);
                    remoteSystemInfo.RemoteSystem = e;
                    RemoteSystemInfoCollection.Insert(index, remoteSystemInfo);
                }
            });
        }

        private void RefreshDevices()
        {
            DetailContentPresenter.Visibility = Visibility.Collapsed;
            if (_registeredHandlers == false)
            {
                PingPong.OnAppServiceConnected += PingPong_OnAppServiceConnected;
                PingPong.OnRemoteDeviceAdded += PingPong_OnRemoteDeviceAdded;
                PingPong.OnRemoteDeviceUpdated += PingPong_OnRemoteDeviceUpdated;
                PingPong.OnRemoteDeviceRemoved += PingPong_OnRemoteDeviceRemoved;
                PingPong.OnStatusUpdateMessage += PingPong_OnStatusUpdateMessage;
                PingPong.OnErrorMessage += PingPong_OnErrorMessage;

                _registeredHandlers = true;
            }

            RemoteSystemInfoCollection.Clear();
            PingPong.DiscoverDevices();
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            RefreshDevices();
        }

        private async void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            PackageVersion pv = Package.Current.Id.Version;
            var versionString = $"Version {pv.Major}.{pv.Minor}.{pv.Build}.{pv.Revision}";

            var dialog = new MessageDialog(versionString);
            await dialog.ShowAsync();
        }

        private void TypeComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var typeFilterString = e.AddedItems[0] as string;

            switch (typeFilterString)
            {
                case "Any":
                    PingPong.DiscoveryFilter = new RemoteSystemDiscoveryTypeFilter(RemoteSystemDiscoveryType.Any);
                    break;
                case "Cloud":
                    PingPong.DiscoveryFilter = new RemoteSystemDiscoveryTypeFilter(RemoteSystemDiscoveryType.Cloud);
                    break;
                case "Proximal":
                    PingPong.DiscoveryFilter = new RemoteSystemDiscoveryTypeFilter(RemoteSystemDiscoveryType.Proximal);
                    break;
                case "Spatially Proximal":
                    PingPong.DiscoveryFilter = new RemoteSystemDiscoveryTypeFilter(RemoteSystemDiscoveryType.SpatiallyProximal);
                    break;
                default:
                    PingPong.DiscoveryFilter = new RemoteSystemDiscoveryTypeFilter(RemoteSystemDiscoveryType.Any);
                    break;
            }

            RefreshDevices();
        }

        private void UserComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var userFilterString = e.AddedItems[0] as string;

            switch (userFilterString)
            {
                case "Same User":
                    PingPong.AutorizationFilter = new RemoteSystemAuthorizationKindFilter(RemoteSystemAuthorizationKind.SameUser);
                    break;
                case "Anonymous":
                    PingPong.AutorizationFilter = new RemoteSystemAuthorizationKindFilter(RemoteSystemAuthorizationKind.Anonymous);
                    break;
                default:
                    PingPong.AutorizationFilter = null;
                    break;
            }

            RefreshDevices();
        }

        private void KindComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var kindFilterString = e.AddedItems[0] as string;

            switch (kindFilterString)
            {
                case "Any":
                    PingPong.KindFilter = null;
                    break;
                case "Desktop":
                    PingPong.KindFilter = new RemoteSystemKindFilter(new string[] {RemoteSystemKinds.Desktop});
                    break;
                case "Holographic":
                    PingPong.KindFilter = new RemoteSystemKindFilter(new string[] { RemoteSystemKinds.Holographic });
                    break;
                case "Hub":
                    PingPong.KindFilter = new RemoteSystemKindFilter(new string[] { RemoteSystemKinds.Hub });
                    break;
                case "Phone":
                    PingPong.KindFilter = new RemoteSystemKindFilter(new string[] { RemoteSystemKinds.Phone });
                    break;
                case "Xbox":
                    PingPong.KindFilter = new RemoteSystemKindFilter(new string[] { RemoteSystemKinds.Xbox });
                    break;
                default:
                    PingPong.KindFilter = null;
                    break;
            }

            RefreshDevices();
        }

        private void LogsButton_OnClick(object sender, RoutedEventArgs e)
        {
            StatusMessageListView.Visibility = StatusMessageListView.Visibility == Visibility.Collapsed
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        private void Homeburger_OnClick(object sender, RoutedEventArgs e)
        {
            HomeburgerPane.SamplesSplitView.IsPaneOpen = !HomeburgerPane.SamplesSplitView.IsPaneOpen;
        }

        private void ShareAcrossDevicesSettingStateButton_Click(object sender, RoutedEventArgs e)
        {
            // Check the state of the setting on the local device
            if (!RemoteSystem.IsAuthorizationKindEnabled(RemoteSystemAuthorizationKind.Anonymous))
            {
                // Display message informing user that setting is "everyone"
                SettingStateTextBlock.Text = "Setting is My Devices";
            }
            else
            {
                // Display message informing user that setting is "my devices"
                SettingStateTextBlock.Text = "Setting is Everyone";
            }
        }
    }
}
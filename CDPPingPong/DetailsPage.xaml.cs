﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace CDPPingPong
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class DetailsPage : Page
    {
        public RemoteSystemInfo SelectedSystem { set; get; }
        public DetailsPage()
        {
            this.InitializeComponent();
            this.Loaded += OnLoaded;
            this.Unloaded += OnUnloaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (ShouldGoToWideState())
            {
                // We shouldn't see this page since we are in "wide master-detail" mode.
                // Play a transition as we are navigating from a separate page.
                NavigateBackForWideState(useTransition: true);
            }
            else
            {
                // Realize the main page content.
                FindName("RootPanel");
            }

            Window.Current.SizeChanged += OnWindowSizeChanged;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            SelectedSystem = e.Parameter as RemoteSystemInfo;
            // Register for hardware and software back request from the system
            SystemNavigationManager systemNavigationManager = SystemNavigationManager.GetForCurrentView();
            systemNavigationManager.BackRequested += OnBackRequested;
            systemNavigationManager.AppViewBackButtonVisibility = AppViewBackButtonVisibility.Visible;
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);

            SystemNavigationManager systemNavigationManager = SystemNavigationManager.GetForCurrentView();
            systemNavigationManager.BackRequested -= OnBackRequested;
            systemNavigationManager.AppViewBackButtonVisibility = AppViewBackButtonVisibility.Collapsed;
        }

        private void OnBackRequested(object sender, BackRequestedEventArgs e)
        {
            // Mark event as handled so we don't get bounced out of the app.
            e.Handled = true;
            // Page above us will be our master view.
            // Make sure we are using the "drill out" animation in this transition.
            Frame.Navigate(typeof(MainPage), "Back", new EntranceNavigationTransitionInfo());
        }

        void NavigateBackForWideState(bool useTransition)
        {
            // Evict this page from the cache as we may not need it again.
            NavigationCacheMode = NavigationCacheMode.Disabled;

            if (useTransition)
            {
                Frame.GoBack(new EntranceNavigationTransitionInfo());
            }
            else
            {
                Frame.GoBack(new SuppressNavigationTransitionInfo());
            }
        }

        private bool ShouldGoToWideState()
        {
            return Window.Current.Bounds.Width >= 720;
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            Window.Current.SizeChanged -= OnWindowSizeChanged;
        }

        private void OnWindowSizeChanged(object sender, Windows.UI.Core.WindowSizeChangedEventArgs e)
        {
            if (ShouldGoToWideState())
            {
                // Make sure we are no longer listening to window change events.
                Window.Current.SizeChanged -= OnWindowSizeChanged;

                // We shouldn't see this page since we are in "wide master-detail" mode.
                NavigateBackForWideState(useTransition: false);
            }
        }

        private async void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            PackageVersion pv = Package.Current.Id.Version;
            var versionString = $"Version {pv.Major}.{pv.Minor}.{pv.Build}.{pv.Revision}";

            var dialog = new MessageDialog(versionString);
            await dialog.ShowAsync();
        }

        private void RemoteSystemDetailsControl_Loaded(object sender, RoutedEventArgs e) {}
    }
}

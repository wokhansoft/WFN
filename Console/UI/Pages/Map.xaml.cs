﻿using Microsoft.Maps.MapControl.WPF;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Threading;

using Wokhan.WindowsFirewallNotifier.Common.Net.IP;
using Wokhan.WindowsFirewallNotifier.Console.Helpers;
using Wokhan.WindowsFirewallNotifier.Console.Helpers.ViewModels;
using Wokhan.WindowsFirewallNotifier.Console.UI.Controls;

namespace Wokhan.WindowsFirewallNotifier.Console.UI.Pages
{
    /// <summary>
    /// Logique d'interaction pour Map.xaml
    /// </summary>
    public partial class Map : TimerBasedPage
    {
        public Location CurrentCoordinates { get; private set; }

        private bool _isFullRouteDisplayed;
        public bool IsFullRouteDisplayed
        {
            get { return _isFullRouteDisplayed; }
            set { _isFullRouteDisplayed = value; NotifyPropertyChanged(nameof(IsFullRouteDisplayed)); }
        }

        public bool IsAerial
        {
            get { return _mode is AerialMode; }
            set { Mode = (value ? new AerialMode(true) : (MapMode)new RoadMode()); NotifyPropertyChanged(nameof(IsAerial)); }
        }

        private MapMode _mode = new RoadMode();
        public MapMode Mode
        {
            get { return _mode; }
            set { _mode = value; NotifyPropertyChanged(nameof(Mode)); }
        }

        public ObservableCollection<MapGroupedView> Connections { get; } = new ObservableCollection<MapGroupedView>();

        public ListCollectionView ConnectionsView { get; }
        public ObservableCollection<GeoConnection2> ConnectionsRoutes { get; } = new ObservableCollection<GeoConnection2>();

        
        public event PropertyChangedEventHandler PropertyChanged;

        protected void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }


        public Map()
        {
            this.Loaded += Map_Loaded;
            
            ConnectionsView = (ListCollectionView)CollectionViewSource.GetDefaultView(ConnectionsRoutes);
            ConnectionsView.IsLiveGrouping = true;
            ConnectionsView.GroupDescriptions.Add(new PropertyGroupDescription("Owner"));

            InitializeComponent();

        }

        async void Map_Loaded(object sender, RoutedEventArgs e)
        {

            if (!GeoConnection2.CheckDB())
            {
                MessageBox.Show("The IP database cannot be found. The Map feature is disabled.", "Missing database");
                return;
            }

            await GeoConnection2.InitCache().ConfigureAwait(true);

            try
            {
                CurrentCoordinates = GeoConnection2.CurrentCoordinates;
            }
            catch
            {
                // TODO: add log
                CurrentCoordinates = new Location(0, 0);
            }

            initialPoint.SetValue(MapLayer.PositionProperty, CurrentCoordinates);

            ProgressStack.Visibility = Visibility.Collapsed;
        }

        protected override async Task OnTimerTick(object sender, EventArgs e)
        {
            foreach (var c in IPHelper.GetAllConnections(true)
                                      .Where(co => co.State == ConnectionStatus.ESTABLISHED && !co.IsLoopback && co.OwnerModule != null))
            {
                AddOrUpdateConnection(c);
            }

            CurrentMap.UpdateLayout();
            /*
            var killduration = Math.Max(5, 3 * _interval);
            var dieduration = Math.Max(2, 2 * _interval);
            for (int i = Connections.Count - 1; i >= 0; i--)
            {
                var item = Connections[i];
                double elapsed = DateTime.Now.Subtract(item.LastSeen).TotalSeconds;
                if (elapsed > killduration)
                {
                    Connections.Remove(item);
                }
                else if (elapsed > dieduration)
                {
                    item.IsDying = true;
                }
            }*/
        }

        private void AddOrUpdateConnection(IConnectionOwnerInfo b)
        {
            var ic = ConnectionsRoutes.Count % LineChart.ColorsDic.Count;
            var br = new SolidColorBrush(LineChart.ColorsDic[ic]);

            GeoConnection2 existingRoute = ConnectionsRoutes.SingleOrDefault(l => l.RemoteAddress.Equals(b.RemoteAddress));
            if (existingRoute is null)
            {
                ConnectionsRoutes.Add(new GeoConnection2(b) { Brush = br });
            }
        }

        private void btnGrpOwner_Checked(object sender, RoutedEventArgs e)
        {
            ConnectionsView.GroupDescriptions.Clear();
            ConnectionsView.GroupDescriptions.Add(new PropertyGroupDescription("Owner"));
        }

        private void btnGrpIP_Checked(object sender, RoutedEventArgs e)
        {
            ConnectionsView.GroupDescriptions.Clear();
            ConnectionsView.GroupDescriptions.Add(new PropertyGroupDescription("RemoteAddress"));
        }
    }
}
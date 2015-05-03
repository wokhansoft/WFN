﻿using MahApps.Metro.Controls.Dialogs;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;

namespace WFNConsole.UI.Windows
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        public bool IsAdmin { get { return false; } }

        public MainWindow()
        {
            InitializeComponent();

            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            Application.Current.DispatcherUnhandledException += Current_DispatcherUnhandledException;
        }

        private void btnConnections_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            mainFrame.Navigate(new Uri("/UI/Pages/Connections.xaml", UriKind.Relative));
        }

        private void btnRules_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            mainFrame.Navigate(new Uri("/UI/Pages/Rules.xaml", UriKind.Relative));
        }

        private void btnEventsLog_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            mainFrame.Navigate(new Uri("/UI/Pages/EventsLog.xaml", UriKind.Relative));
        }

        private void btnAbout_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            flyAbout.IsOpen = true;
        }

        private void btnDonate_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Process.Start("https://www.paypal.com/cgi-bin/webscr?cmd=_donations&business=wokhan%40online%2efr&lc=US&item_name=Khan%20%28Windows%20Firewall%20Notifier%29&item_number=WOK%2dWFN&currency_code=EUR&bn=PP%2dDonationsBF%3abtn_donateCC_LG%2egif%3aNonHosted");
        }

        void Current_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            if (e.Exception is InvalidOperationException && e.Exception.InnerException != null && e.Exception.InnerException is Win32Exception && ((Win32Exception)e.Exception.InnerException).NativeErrorCode == 1314)
            {
                ForceDialog("You must run the Windows Firewall Notifier console as an administrator to be able to use this feature.", "Insufficiant privileges");
            }
            else
            {
                ForceDialog(e.Exception.Message, "Unexpected error");
            }
            e.Handled = true;
        }

        private async void ForceDialog(string p1, string p2)
        {
            var dial = await this.GetCurrentDialogAsync<BaseMetroDialog>();

            if (dial != null)
            {
                dial.Title = p2;
                dial.Content = p1;
                await this.HideMetroDialogAsync(dial);
            }
            else
            {
                await this.ShowMessageAsync(p2, p1, MessageDialogStyle.Affirmative);
            }
        }

        void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            ForceDialog(((Exception)e.ExceptionObject).Message, "Unexpected error");
            //this.ShowMessageAsync("Unexpected error", ((Exception)e.ExceptionObject).Message, MessageDialogStyle.Affirmative);
        }
    }
}

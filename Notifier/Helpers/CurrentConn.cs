﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace Wokhan.WindowsFirewallNotifier.Notifier.Helpers
{
    public class CurrentConn : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public string CurrentProd { get; set; }

        private ImageSource _icon;
        public ImageSource Icon
        {
            get { return _icon; }
            set { _icon = value; }// NotifyPropertyChanged("Icon"); }

        }

        public string CurrentPath { get; set; }
        public string CurrentService { get; set; }
        public string CurrentServiceDesc { get; set; }
        public string RuleName { get; set; }
        public string LocalPort { get; set; }
        public string Target { get; set; }
        public string TargetPort { get; set; }
        public int Protocol { get; set; }

        private string _resolvedHost = null;
        public string ResolvedHost
        {
            get { return _resolvedHost; }
            set { _resolvedHost = value;  }//NotifyPropertyChanged("ResolvedHost"); }
        }

        public string[] PossibleServices { get; set; }
        public string[] PossibleServicesDesc { get; set; }
    }
}

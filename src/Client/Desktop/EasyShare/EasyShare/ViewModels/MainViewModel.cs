using EasyShare.Api;
using EasyShare.Core;
using System.Collections.ObjectModel;

namespace EasyShare.ViewModels
{
    public class MainViewModel : ObservableObject
    {
        private IClipboardMonitor monitor;
        private EasyShareHandler esHandler;

        public MainViewModel(EasyShareHandler esHandler, IClipboardMonitor monitor)
        {
            this.esHandler = esHandler;
            this.monitor = monitor;
        }

        public ObservableCollection<string> Logs { get; set; }

        public string Id
        {
            get { return GetProperty<string>(); }
            set
            {
                if (SetProperty(value))
                {
                    esHandler.UpdateId(value);
                }
            }
        }

        public string Status
        {
            get { return GetProperty<string>(); }
            set { SetProperty(value); }
        }

        public bool IsListening
        {
            get { return GetProperty<bool>(); }
            set
            {
                if (SetProperty(value))
                {
                    if (value) { monitor.Start(); } else { monitor.Stop(); }
                }
            }
        }

        public bool IsSyncing
        {
            get { return GetProperty<bool>(); }
            set
            {
                if (SetProperty(value))
                {
                    if (value) { esHandler.StartFetching(); } else { esHandler.StopFetching(); }
                }
            }
        }
    }
}

using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace EasyShare.Core
{
    public class ClipboardMonitor : IClipboardMonitor
    {
        private const int WM_CLIPBOARDUPDATE = 0x031D;
        private IntPtr hwnd;

        public event Action ClipboardUpdated;

        public ClipboardMonitor(Window window)
        {
            window.SourceInitialized += (sender, e) =>
            {
                var hwndSource = PresentationSource.FromVisual(window) as HwndSource;
                if (hwndSource != null)
                {
                    hwnd = hwndSource.Handle;
                    hwndSource.AddHook(WndProc);
                    window.Closing += (sender2, o) => RemoveClipboardFormatListener(hwnd);
                }
            };
        }

        public void Start()
        {
            AddClipboardFormatListener(hwnd);
        }

        public void Stop()
        {
            RemoveClipboardFormatListener(hwnd);
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_CLIPBOARDUPDATE)
            {
                var handler = ClipboardUpdated;
                if (handler != null)
                {
                    handler();
                }
            }

            return IntPtr.Zero;
        }

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool AddClipboardFormatListener(IntPtr hwnd);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool RemoveClipboardFormatListener(IntPtr hwnd);
    }
}

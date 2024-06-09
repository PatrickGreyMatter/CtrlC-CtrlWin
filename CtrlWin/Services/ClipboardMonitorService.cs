using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using CtrlWin.Data;
using CtrlWin.Models;
using Application = System.Windows.Application;
using ApplicationForms = System.Windows.Forms.Application;

namespace CtrlWin.Services
{
    public class ClipboardMonitorService : IDisposable
    {
        private readonly ClipboardDbContext _context;
        private readonly HiddenWindow _hiddenWindow;

        public ClipboardMonitorService(ClipboardDbContext context)
        {
            _context = context;

            _hiddenWindow = new HiddenWindow();
            _hiddenWindow.ClipboardChanged += OnClipboardChanged;
            _hiddenWindow.Show();
        }

        private void OnClipboardChanged(object sender, EventArgs e)
        {
            if (System.Windows.Clipboard.ContainsText())
            {
                var text = System.Windows.Clipboard.GetText();
                SaveTextToDatabase(text);
            }
        }

        private void SaveTextToDatabase(string text)
        {
            var textItem = new TextItem
            {
                Content = text,
                DateSaved = DateTime.Now,
                Size = text.Length,
                Name = "Copied Text"
            };

            _context.TextItems.Add(textItem);
            _context.SaveChanges();

            // Reload the items in the main window
            Application.Current.Dispatcher.Invoke(() =>
            {
                ((MainWindow)Application.Current.MainWindow).LoadTextItems();
            });
        }

        public void Dispose()
        {
            _hiddenWindow.ClipboardChanged -= OnClipboardChanged;
            _hiddenWindow.Close();
        }

        private class HiddenWindow : Window
        {
            public event EventHandler ClipboardChanged;

            public HiddenWindow()
            {
                Loaded += (s, e) =>
                {
                    var source = HwndSource.FromHwnd(new WindowInteropHelper(this).Handle);
                    source.AddHook(WndProc);
                    NativeMethods.AddClipboardFormatListener(source.Handle);
                };

                // Hide the window from the taskbar and make it invisible
                this.ShowInTaskbar = false;
                this.WindowState = WindowState.Minimized;
                this.Visibility = Visibility.Hidden;
            }

            private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
            {
                if (msg == NativeMethods.WM_CLIPBOARDUPDATE)
                {
                    ClipboardChanged?.Invoke(this, EventArgs.Empty);
                }
                return IntPtr.Zero;
            }
        }


        private static class NativeMethods
        {
            public const int WM_CLIPBOARDUPDATE = 0x031D;

            [DllImport("user32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool AddClipboardFormatListener(IntPtr hwnd);
        }
    }
}

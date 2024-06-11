using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using CtrlWin.Data;
using CtrlWin.Models;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;

namespace CtrlWin.Services
{
    public class ClipboardMonitorService : IDisposable
    {
        private readonly ClipboardDbContext _context;
        private readonly HiddenWindow _hiddenWindow;

        public event EventHandler ClipboardChanged = delegate { };

        public ClipboardMonitorService(ClipboardDbContext context)
        {
            _context = context;

            _hiddenWindow = new HiddenWindow();
            _hiddenWindow.ClipboardChanged += OnClipboardChanged;
            _hiddenWindow.Show();
        }

        private void OnClipboardChanged(object? sender, EventArgs? e)
        {
            if (System.Windows.Clipboard.ContainsText())
            {
                var text = System.Windows.Clipboard.GetText();
                if (!IsTextInDatabase(text))
                {
                    SaveTextToDatabase(text);
                }
            }
            else if (System.Windows.Clipboard.ContainsImage())
            {
                var image = System.Windows.Clipboard.GetImage();
                var filePath = SaveImageToFile(image);
                if (!IsImageInDatabase(filePath))
                {
                    SaveImageToDatabase(filePath);
                }
            }
        }

        private string SaveImageToFile(BitmapSource image)
        {
            var directoryPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images");
            var fileName = $"Image_{DateTime.Now:yyyyMMddHHmmss}.png";
            var filePath = Path.Combine(directoryPath, fileName);

            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            try
            {
                using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    var encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(image));
                    encoder.Save(fileStream);
                }
            }
            catch (Exception ex)
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
                throw new Exception("Failed to save image", ex);
            }

            return filePath;
        }

        private bool IsImageInDatabase(string filePath)
        {
            return _context.ImageItems.Any(i => i.FilePath == filePath);
        }

        private void SaveImageToDatabase(string filePath)
        {
            var imageItem = new ImageItem
            {
                FilePath = filePath,
                DateSaved = DateTime.Now,
                Size = new FileInfo(filePath).Length,
                Name = "Copied Image"
            };

            _context.ImageItems.Add(imageItem);
            _context.SaveChanges();

            Application.Current.Dispatcher.Invoke(() =>
            {
                ((MainWindow)Application.Current.MainWindow).LoadImageItems();
            });
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

        private bool IsTextInDatabase(string text)
        {
            return _context.TextItems.Any(t => t.Content == text);
        }

        public void Dispose()
        {
            _hiddenWindow.ClipboardChanged -= OnClipboardChanged;
            _hiddenWindow.Close();
        }

        private class HiddenWindow : Window
        {
            public event EventHandler ClipboardChanged = delegate { };

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

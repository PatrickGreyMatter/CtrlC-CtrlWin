using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Input;
using System.Windows.Media;
using CtrlWin.Data;
using CtrlWin.Models;
using CtrlWin.Services;
using Microsoft.EntityFrameworkCore;
using System.Windows.Forms; // Use the fully qualified name for WinForms
using MessageBox = System.Windows.MessageBox;
using System.IO; // Explicitly specify MessageBox to avoid ambiguity

namespace CtrlWin
{
    public partial class MainWindow : Window
    {
        private readonly ClipboardDbContext _context;
        private readonly ClipboardMonitorService _clipboardMonitorService;

        public MainWindow()
        {
            InitializeComponent();
            _context = new ClipboardDbContext();

            // Ensure the database is up-to-date
            _context.Database.EnsureCreated();

            // Start clipboard monitoring
            _clipboardMonitorService = new ClipboardMonitorService(_context);

            LoadTextItems();
        }

        public void LoadTextItems()
        {
            try
            {
                var textItems = _context.TextItems.OrderByDescending(t => t.DateSaved).ToList();
                ClipboardListBox.ItemsSource = textItems;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading text items: {ex.Message}");
            }
        }

        public void LoadImageItems()
        {
            try
            {
                var imageItems = _context.ImageItems.OrderByDescending(i => i.DateSaved).ToList();
                ClipboardListBox.ItemsSource = imageItems;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading image items: {ex.Message}");
            }
        }

        public void LoadVideoItems()
        {
            try
            {
                var videoItems = _context.VideoItems.OrderByDescending(v => v.DateSaved).ToList();
                ClipboardListBox.ItemsSource = videoItems;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading video items: {ex.Message}");
            }
        }

        private void ClipboardListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ClipboardListBox.SelectedItem is TextItem textItem && !string.IsNullOrWhiteSpace(textItem.Content))
            {
                FullTextBox.Text = textItem.Content;
                FullTextBox.Visibility = Visibility.Visible;
                FullImageBox.Visibility = Visibility.Collapsed;
                FullVideoBox.Visibility = Visibility.Collapsed;
                CollapseButton.Visibility = Visibility.Visible;
            }
            else if (ClipboardListBox.SelectedItem is ImageItem imageItem && !string.IsNullOrWhiteSpace(imageItem.FilePath))
            {
                try
                {
                    if (File.Exists(imageItem.FilePath))
                    {
                        var bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.UriSource = new Uri(imageItem.FilePath, UriKind.Absolute);
                        bitmap.EndInit();
                        FullImageBox.Source = bitmap;
                        FullImageBox.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        FullImageBox.Source = null;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading image: {ex.Message}");
                    FullImageBox.Source = null;
                }

                FullTextBox.Visibility = Visibility.Collapsed;
                FullVideoBox.Visibility = Visibility.Collapsed;
                CollapseButton.Visibility = Visibility.Visible;
            }
            else if (ClipboardListBox.SelectedItem is VideoItem videoItem && !string.IsNullOrWhiteSpace(videoItem.FilePath))
            {
                try
                {
                    if (File.Exists(videoItem.FilePath))
                    {
                        FullVideoBox.Source = new Uri(videoItem.FilePath, UriKind.Absolute);
                        FullVideoBox.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        FullVideoBox.Source = null;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading video: {ex.Message}");
                    FullVideoBox.Source = null;
                }

                FullTextBox.Visibility = Visibility.Collapsed;
                FullImageBox.Visibility = Visibility.Collapsed;
                CollapseButton.Visibility = Visibility.Visible;
            }
            else
            {
                FullTextBox.Visibility = Visibility.Collapsed;
                FullImageBox.Visibility = Visibility.Collapsed;
                FullVideoBox.Visibility = Visibility.Collapsed;
                CollapseButton.Visibility = Visibility.Collapsed;
            }
        }






        private void FullImageBox_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Delta > 0)
            {
                FullImageBox.LayoutTransform = new ScaleTransform(1.2, 1.2);
            }
            else
            {
                FullImageBox.LayoutTransform = new ScaleTransform(1.0, 1.0);
            }
        }

        private void CollapseButton_Click(object sender, RoutedEventArgs e)
        {
            FullTextBox.Visibility = Visibility.Collapsed;
            FullImageBox.Visibility = Visibility.Collapsed;
            FullVideoBox.Visibility = Visibility.Collapsed;
            CollapseButton.Visibility = Visibility.Collapsed;
        }

        protected override void OnClosed(EventArgs e)
        {
            _clipboardMonitorService.Dispose();
            base.OnClosed(e);
        }

        private void TextsButton_Click(object sender, RoutedEventArgs e)
        {
            LoadTextItems();
        }

        private void ImagesButton_Click(object sender, RoutedEventArgs e)
        {
            LoadImageItems();
        }

        private void VideosButton_Click(object sender, RoutedEventArgs e)
        {
            LoadVideoItems();
        }

        private void CopyButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement frameworkElement && frameworkElement.Tag is int id)
            {
                try
                {
                    if (ClipboardListBox.SelectedItem is TextItem textItem)
                    {
                        var item = _context.TextItems.Find(id);
                        if (item != null)
                        {
                            System.Windows.Forms.Clipboard.SetText(item.Content);
                        }
                    }
                    else if (ClipboardListBox.SelectedItem is ImageItem imageItem)
                    {
                        var item = _context.ImageItems.Find(id);
                        if (item != null)
                        {
                            System.Windows.Forms.Clipboard.SetImage(System.Drawing.Image.FromFile(item.FilePath));
                        }
                    }
                    else if (ClipboardListBox.SelectedItem is VideoItem videoItem)
                    {
                        var item = _context.VideoItems.Find(id);
                        if (item != null)
                        {
                            System.Windows.Forms.Clipboard.SetFileDropList(new System.Collections.Specialized.StringCollection { item.FilePath });
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error copying item: {ex.Message}");
                }
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement frameworkElement && frameworkElement.Tag is int id)
            {
                try
                {
                    if (ClipboardListBox.SelectedItem is TextItem textItem)
                    {
                        var item = _context.TextItems.Find(id);
                        if (item != null)
                        {
                            _context.TextItems.Remove(item);
                        }
                    }
                    else if (ClipboardListBox.SelectedItem is ImageItem imageItem)
                    {
                        var item = _context.ImageItems.Find(id);
                        if (item != null)
                        {
                            _context.ImageItems.Remove(item);
                        }
                    }
                    else if (ClipboardListBox.SelectedItem is VideoItem videoItem)
                    {
                        var item = _context.VideoItems.Find(id);
                        if (item != null)
                        {
                            _context.VideoItems.Remove(item);
                        }
                    }

                    _context.SaveChanges();
                    LoadTextItems();  // Adjust this to reload the appropriate items
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error deleting item: {ex.Message}");
                }
            }
        }
    }
}

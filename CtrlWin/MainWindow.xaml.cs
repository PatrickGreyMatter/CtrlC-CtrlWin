using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CtrlWin.Data;
using CtrlWin.Models;
using CtrlWin.Services;
using Microsoft.EntityFrameworkCore;
using MessageBox = System.Windows.MessageBox;
using System.Windows.Forms;

namespace CtrlWin
{
    public partial class MainWindow : Window
    {
        private readonly ClipboardDbContext _context;
        private readonly ClipboardMonitorService _clipboardMonitorService;
        private ScaleTransform scaleTransform = new ScaleTransform(1.0, 1.0);

        public MainWindow()
        {
            InitializeComponent();
            _context = new ClipboardDbContext();

            _context.Database.EnsureCreated();

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

                        // Fit image to container
                        FitImageToContainer();
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

            // Reset zoom level and scroll position
            ResetImageTransform();
        }

        private void ResetImageTransform()
        {
            scaleTransform = new ScaleTransform(1.0, 1.0);
            FullImageBox.LayoutTransform = scaleTransform;

            var scrollViewer = FindVisualChild<ScrollViewer>(FullImageBox);
            if (scrollViewer != null)
            {
                scrollViewer.ScrollToHorizontalOffset(0);
                scrollViewer.ScrollToVerticalOffset(0);
            }
        }

        private void FitImageToContainer()
        {
            if (FullImageBox.Source != null)
            {
                var imageSource = (BitmapSource)FullImageBox.Source;

                double containerWidth = FullImageBox.ActualWidth;
                double containerHeight = FullImageBox.ActualHeight;

                double imageWidth = imageSource.PixelWidth;
                double imageHeight = imageSource.PixelHeight;

                double scaleX = containerWidth / imageWidth;
                double scaleY = containerHeight / imageHeight;

                double scale = Math.Min(scaleX, scaleY);

                scaleTransform = new ScaleTransform(scale, scale);
                FullImageBox.LayoutTransform = scaleTransform;

                // Reset scroll position
                var scrollViewer = FindVisualChild<ScrollViewer>(FullImageBox);
                if (scrollViewer != null)
                {
                    scrollViewer.ScrollToHorizontalOffset(0);
                    scrollViewer.ScrollToVerticalOffset(0);
                }
            }
        }

        private void FullImageBox_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Shift)
            {
                e.Handled = true; // Prevent default scrolling behavior

                var image = (System.Windows.Controls.Image)sender;

                // Get the current scale transform
                var transform = (ScaleTransform)image.LayoutTransform;

                // Define zoom intensity and minimum/maximum scale
                double zoomIntensity = 0.1;
                double minScale = 0.1; // Adjusted minimum scale to allow zooming out to a smaller size
                double maxScale = 10.0;

                // Calculate new scale
                double newScale = transform.ScaleX + (e.Delta > 0 ? zoomIntensity : -zoomIntensity);

                // Limit the scale within the min and max scale
                newScale = Math.Max(minScale, Math.Min(maxScale, newScale));

                // Set the new scale
                transform.ScaleX = transform.ScaleY = newScale;

                // Update the image layout transform
                image.LayoutTransform = transform;
            }
        }

        private T FindVisualChild<T>(DependencyObject obj) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(obj, i);
                if (child != null && child is T)
                    return (T)child;
                else
                {
                    T childOfChild = FindVisualChild<T>(child);
                    if (childOfChild != null)
                        return childOfChild;
                }
            }
            return null;
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

        private void ChooseFolderButton_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                DialogResult result = dialog.ShowDialog();

                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    Properties.Settings.Default.ImageFolderPath = dialog.SelectedPath;
                    Properties.Settings.Default.Save();
                }
            }
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            var settingsListBox = new System.Windows.Controls.ListBox();

            var settingsOptions = new List<string> { "Choisir le dossier Images", "Choisir le dossier Videos" };

            foreach (var option in settingsOptions)
            {
                settingsListBox.Items.Add(option);
            }

            settingsListBox.SelectionChanged += SettingsListBox_SelectionChanged;

            settingsListBox.FontSize = 16;

            var window = new Window
            {
                Content = settingsListBox,
                Width = 200,
                Height = 300,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                ResizeMode = ResizeMode.NoResize
            };

            window.ShowDialog();
        }

        private void SettingsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is System.Windows.Controls.ListBox listBox && listBox.SelectedItem is string selectedOption)
            {
                switch (selectedOption)
                {
                    case "Choisir le dossier Images":
                        ChooseImageFolder();
                        break;
                    case "Choisir le dossier Videos":
                        ChooseVideoFolderButton_Click(sender, e);
                        break;
                }
            }
        }

        private void CopyButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement frameworkElement && frameworkElement.Tag is object item)
            {
                try
                {
                    if (item is TextItem textItem)
                    {
                        _clipboardMonitorService.CopyTextToClipboard(textItem.Content);
                    }
                    else if (item is ImageItem imageItem)
                    {
                        var bitmap = new BitmapImage(new Uri(imageItem.FilePath));
                        _clipboardMonitorService.CopyImageToClipboard(bitmap);
                    }
                    else if (item is VideoItem videoItem)
                    {
                        var fileCollection = new System.Collections.Specialized.StringCollection();
                        fileCollection.Add(videoItem.FilePath);
                        System.Windows.Forms.Clipboard.SetFileDropList(fileCollection);
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
            if (sender is FrameworkElement frameworkElement && frameworkElement.Tag is object item)
            {
                try
                {
                    if (item is TextItem textItem)
                    {
                        var entity = _context.TextItems.Find(textItem.Id);
                        if (entity != null)
                        {
                            _context.TextItems.Remove(entity);
                        }
                    }
                    else if (item is ImageItem imageItem)
                    {
                        var entity = _context.ImageItems.Find(imageItem.Id);
                        if (entity != null)
                        {
                            if (File.Exists(imageItem.FilePath))
                            {
                                File.Delete(imageItem.FilePath);
                            }
                            _context.ImageItems.Remove(entity);
                        }
                    }
                    else if (item is VideoItem videoItem)
                    {
                        var entity = _context.VideoItems.Find(videoItem.Id);
                        if (entity != null)
                        {
                            if (File.Exists(videoItem.FilePath))
                            {
                                File.Delete(videoItem.FilePath);
                            }
                            _context.VideoItems.Remove(entity);
                        }
                    }

                    _context.SaveChanges();
                    LoadVideoItems(); // Adjust this method to refresh the list appropriately.
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error deleting item: {ex.Message}");
                }
            }
        }

        private void ChooseImageFolder()
        {
            string imagePath = Properties.Settings.Default.ImageFolderPath;

            if (!string.IsNullOrEmpty(imagePath))
            {
                MessageBoxResult result = MessageBox.Show($"Le dossier d'enregistrement actuel pour les images est : {imagePath}. Voulez-vous le modifier ?", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    using (var dialog = new FolderBrowserDialog())
                    {
                        DialogResult dialogResult = dialog.ShowDialog();

                        if (dialogResult == System.Windows.Forms.DialogResult.OK)
                        {
                            Properties.Settings.Default.ImageFolderPath = dialog.SelectedPath;
                            Properties.Settings.Default.Save();
                        }
                    }
                }
            }
            else
            {
                using (var dialog = new FolderBrowserDialog())
                {
                    DialogResult dialogResult = dialog.ShowDialog();

                    if (dialogResult == System.Windows.Forms.DialogResult.OK)
                    {
                        Properties.Settings.Default.ImageFolderPath = dialog.SelectedPath;
                        Properties.Settings.Default.Save();
                    }
                }
            }
        }

        private void ChooseVideoFolderButton_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                DialogResult result = dialog.ShowDialog();

                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    Properties.Settings.Default.VideoFolderPath = dialog.SelectedPath;
                    Properties.Settings.Default.Save();
                }
            }
        }

        private void CopyVideoLinkButton_Click(object sender, RoutedEventArgs e)
        {
            string videoUrl = VideoLinkTextBox.Text;
            if (!string.IsNullOrWhiteSpace(videoUrl))
            {
                CopyVideoLink(videoUrl);
            }
            else
            {
                MessageBox.Show("Please enter a valid video URL.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CopyVideoLink(string url)
        {
            var videoDownloader = new VideoDownloaderService(_context);
            videoDownloader.DownloadVideo(url);
        }
    }
}

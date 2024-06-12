using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CtrlWin.Data;
using CtrlWin.Models;
using CtrlWin.Services;
using Microsoft.EntityFrameworkCore;
using Button = System.Windows.Controls.Button;
using MessageBox = System.Windows.MessageBox;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using Orientation = System.Windows.Controls.Orientation;

namespace CtrlWin
{
    public partial class MainWindow : Window
    {
        private readonly ClipboardDbContext _context;
        private readonly ClipboardMonitorService _clipboardMonitorService;
        private ScaleTransform scaleTransform = new ScaleTransform(1.0, 1.0);
        private bool isPaused = false;
        private Window? fullScreenWindow = null;

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

        private void PauseButton_Click(object sender, RoutedEventArgs e)
        {
            if (isPaused)
            {
                FullVideoBox.Play();
                ((Button)sender).Content = "Pause";
                isPaused = false;
            }
            else
            {
                FullVideoBox.Pause();
                ((Button)sender).Content = "Resume";
                isPaused = true;
            }
        }



        private void FullscreenButton_Click(object sender, RoutedEventArgs e)
        {
            if (fullScreenWindow == null)
            {
                fullScreenWindow = new Window
                {
                    WindowStyle = WindowStyle.None,
                    WindowState = WindowState.Maximized,
                    Content = new Grid
                    {
                        Background = new SolidColorBrush(Colors.Black),
                        Children =
                {
                    new MediaElement
                    {
                        Source = FullVideoBox.Source,
                        LoadedBehavior = MediaState.Manual,
                        UnloadedBehavior = MediaState.Manual,
                        Stretch = Stretch.Uniform,
                        Name = "FullScreenVideo"
                    },
                    new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Bottom,
                        Margin = new Thickness(10),
                        Children =
                        {
                            new Button { Content = "Pause", Width = 75, Height = 30, Margin = new Thickness(5) },
                            new Button { Content = "Exit Fullscreen", Width = 100, Height = 30, Margin = new Thickness(5) }
                        }
                    }
                }
                    }
                };

                // Attach event handlers
                ((MediaElement)((Grid)fullScreenWindow.Content).Children[0]).MediaEnded += (s, ev) =>
                {
                    ((MediaElement)fullScreenWindow.Content).Stop();
                    fullScreenWindow.Close();
                    fullScreenWindow = null;
                };

                ((MediaElement)((Grid)fullScreenWindow.Content).Children[0]).MediaOpened += (s, ev) =>
                {
                    ((MediaElement)((Grid)fullScreenWindow.Content).Children[0]).Play();
                };

                // Attach button click event handlers
                var buttons = ((StackPanel)((Grid)fullScreenWindow.Content).Children[1]).Children;
                ((Button)buttons[0]).Click += PauseButton_Click;
                ((Button)buttons[1]).Click += ExitFullscreenButton_Click;

                fullScreenWindow.ShowDialog();
            }
        }





        private void ExitFullscreenButton_Click(object sender, RoutedEventArgs e)
        {
            if (fullScreenWindow != null)
            {
                fullScreenWindow.Close();
                fullScreenWindow = null;
            }
        }



        private void ClipboardListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            StopVideo();

            if (ClipboardListBox.SelectedItem is TextItem textItem && !string.IsNullOrWhiteSpace(textItem.Content))
            {
                FullTextBox.Text = textItem.Content;
                FullTextBox.Visibility = Visibility.Visible;
                FullImageBox.Visibility = Visibility.Collapsed;
                FullVideoContainer.Visibility = Visibility.Collapsed;
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
                FullVideoContainer.Visibility = Visibility.Collapsed;
                CollapseButton.Visibility = Visibility.Visible;
            }
            else if (ClipboardListBox.SelectedItem is VideoItem videoItem && !string.IsNullOrWhiteSpace(videoItem.FilePath))
            {
                try
                {
                    if (File.Exists(videoItem.FilePath))
                    {
                        FullVideoBox.Source = new Uri(videoItem.FilePath, UriKind.Absolute);
                        FullVideoContainer.Visibility = Visibility.Visible;
                        FullVideoBox.Play();
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
                FullVideoContainer.Visibility = Visibility.Collapsed;
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
            StopVideo();

            FullTextBox.Visibility = Visibility.Collapsed;
            FullImageBox.Visibility = Visibility.Collapsed;
            FullVideoContainer.Visibility = Visibility.Collapsed;
            CollapseButton.Visibility = Visibility.Collapsed;
        }

        protected override void OnClosed(EventArgs e)
        {
            _clipboardMonitorService.Dispose();
            base.OnClosed(e);
        }

        private void TextsButton_Click(object sender, RoutedEventArgs e)
        {
            StopVideo();
            LoadTextItems();
        }

        private void ImagesButton_Click(object sender, RoutedEventArgs e)
        {
            StopVideo();
            LoadImageItems();
        }

        private void VideosButton_Click(object sender, RoutedEventArgs e)
        {
            StopVideo();
            LoadVideoItems();
        }

        private void ChooseFolderButton_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                System.Windows.Forms.DialogResult result = dialog.ShowDialog();

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
                    using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
                    {
                        System.Windows.Forms.DialogResult dialogResult = dialog.ShowDialog();

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
                using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
                {
                    System.Windows.Forms.DialogResult dialogResult = dialog.ShowDialog();

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
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                System.Windows.Forms.DialogResult result = dialog.ShowDialog();

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

        private void StopVideo()
        {
            FullVideoBox.Stop();
            FullVideoBox.Source = null;
        }

        private void FullVideoBox_MediaEnded(object sender, RoutedEventArgs e)
        {
            StopVideo();
        }

        private void EditableTextBlock_TextChanged(object sender, RoutedEventArgs e)
        {
            if (sender is EditableTextBlock editableTextBlock)
            {
                if (editableTextBlock.DataContext is ImageItem imageItem)
                {
                    var entity = _context.ImageItems.Find(imageItem.Id);
                    if (entity != null)
                    {
                        entity.Name = editableTextBlock.Text;
                        _context.SaveChanges();
                    }
                }
                else if (editableTextBlock.DataContext is VideoItem videoItem)
                {
                    var entity = _context.VideoItems.Find(videoItem.Id);
                    if (entity != null)
                    {
                        entity.Name = editableTextBlock.Text;
                        _context.SaveChanges();
                    }
                }
            }
        }

        private void RenameButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement frameworkElement && frameworkElement.Tag is object item)
            {
                string newName = Microsoft.VisualBasic.Interaction.InputBox("Enter new name:", "Rename");

                if (!string.IsNullOrWhiteSpace(newName))
                {
                    try
                    {
                        if (item is ImageItem imageItem)
                        {
                            var entity = _context.ImageItems.Find(imageItem.Id);
                            if (entity != null)
                            {
                                entity.Name = newName;
                                _context.SaveChanges();
                                LoadImageItems(); // Refresh the list
                            }
                        }
                        else if (item is VideoItem videoItem)
                        {
                            var entity = _context.VideoItems.Find(videoItem.Id);
                            if (entity != null)
                            {
                                entity.Name = newName;
                                _context.SaveChanges();
                                LoadVideoItems(); // Refresh the list
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error renaming item: {ex.Message}");
                    }
                }
            }
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                this.WindowState = this.WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
            }
            else
            {
                this.DragMove();
            }
        }

    }
}

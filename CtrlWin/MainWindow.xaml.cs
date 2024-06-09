using System;
using System.Linq;
using System.Windows;
using CtrlWin.Data;
using CtrlWin.Models;
using CtrlWin.Services;
using Microsoft.EntityFrameworkCore;
using MessageBox = System.Windows.MessageBox;
using WinForms = System.Windows.Forms;

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

        private void ClipboardListBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (ClipboardListBox.SelectedItem is TextItem selectedItem)
            {
                FullTextBox.Text = selectedItem.Content;
                FullTextPanel.Visibility = Visibility.Visible;
            }
            else
            {
                FullTextPanel.Visibility = Visibility.Collapsed;
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement frameworkElement && frameworkElement.Tag is int id)
            {
                try
                {
                    var textItem = _context.TextItems.Find(id);
                    if (textItem != null)
                    {
                        _context.TextItems.Remove(textItem);
                        _context.SaveChanges();
                        LoadTextItems();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error deleting text item: {ex.Message}");
                }
            }
        }

        private void CopyButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement frameworkElement && frameworkElement.Tag is int id)
            {
                try
                {
                    var textItem = _context.TextItems.Find(id);
                    if (textItem != null)
                    {
                        WinForms.Clipboard.SetText(textItem.Content);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error copying text item: {ex.Message}");
                }
            }
        }

        private void CollapseButton_Click(object sender, RoutedEventArgs e)
        {
            FullTextPanel.Visibility = Visibility.Collapsed;
        }

        protected override void OnClosed(EventArgs e)
        {
            _clipboardMonitorService.Dispose();
            base.OnClosed(e);
        }
    }
}

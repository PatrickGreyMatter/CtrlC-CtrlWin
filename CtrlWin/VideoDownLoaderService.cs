using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using CtrlWin.Data;
using CtrlWin.Models;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;

namespace CtrlWin.Services
{
    public class VideoDownloaderService
    {
        private readonly ClipboardDbContext _context;
        private readonly string ytDlpPath;

        public VideoDownloaderService(ClipboardDbContext context)
        {
            _context = context;
            ytDlpPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Resources\yt-dlp\yt-dlp.exe");
        }

        public void DownloadVideo(string url)
        {
            string videoFolderPath = Properties.Settings.Default.VideoFolderPath;

            if (string.IsNullOrEmpty(videoFolderPath))
            {
                MessageBox.Show("Please choose a video folder in the settings.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                string fileName = $"Video_{DateTime.Now:yyyyMMddHHmmss}.mp4";
                string filePath = Path.Combine(videoFolderPath, fileName);
                string archiveFilePath = Path.Combine(videoFolderPath, "archive.txt");

                var arguments = $"--verbose -ci --download-archive \"{archiveFilePath}\" -o \"{filePath}\" \"{url}\"";

                using (var process = new Process())
                {
                    process.StartInfo.FileName = ytDlpPath;
                    process.StartInfo.Arguments = arguments;
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.RedirectStandardError = true;
                    process.StartInfo.CreateNoWindow = true;

                    process.OutputDataReceived += (sender, args) => Console.WriteLine(args.Data);
                    process.ErrorDataReceived += (sender, args) => Console.WriteLine(args.Data);

                    process.Start();
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();

                    process.WaitForExit();
                }

                var videoItem = new VideoItem
                {
                    FilePath = filePath,
                    DateSaved = DateTime.Now,
                    Size = new FileInfo(filePath).Length,
                    Name = new string(fileName.Take(10).ToArray())
                };

                _context.VideoItems.Add(videoItem);
                _context.SaveChanges();

                Application.Current.Dispatcher.Invoke(() =>
                {
                    ((MainWindow)Application.Current.MainWindow).LoadVideoItems();
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error downloading video: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}

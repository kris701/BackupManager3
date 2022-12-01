using System;   
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Permissions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Xml.Linq;
using BackupManager3.Data;
using BackupManager3.Helpers;
using Nanotek.WPF.WindowsTrayItemFramework;
using static System.Windows.Forms.Design.AxImporter;

namespace BackupManager3.Views
{
    /// <summary>
    /// Interaction logic for MainView.xaml
    /// </summary>
    public partial class MainView : UserControl, TrayWindowSwitchable
    {
        private enum BackupResult { None, Successful, Canceled }
        public UIElement Element { get; }
        public double TargetWidth { get; } = 400;
        public double TargetHeight { get; } = 164;

        private string _logPath = "Logs/";
        private string _currentLogName = "None";
        private EnumerationOptions _options = new EnumerationOptions()
        {
            IgnoreInaccessible = true,
            RecurseSubdirectories = true,
        };
        private CancellationTokenSource _cancelTokenSource = new CancellationTokenSource();
        private static bool _firstRunAfterAdmin = true;

        public MainView()
        {
            InitializeComponent();
            Element = this;
        }

        private void SelectFolders_Click(object sender, RoutedEventArgs e)
        {
            ViewSwitcher.SwitchView(new SelectFoldersView());
        }

        private void ExcludeFoldersButton_Click(object sender, RoutedEventArgs e)
        {
            ViewSwitcher.SwitchView(new ExcludeFoldersView());
        }

        private async void BackupNow_Click(object sender, RoutedEventArgs e)
        {
            await ProcessExecuteBackup();
        }

        public async Task ProcessExecuteBackup()
        {
#if RELEASE
            if (!UACHelper.IsProcessElevated)
            {
                MainWindow.SaveContext.Save();
                UACHelper.ElevateProcess("isAdmin");
            }
            else
            {
#endif
                SelectFoldersButton.IsEnabled = false;
                ExcludeFoldersButton.IsEnabled = false;
                BackupNowButton.IsEnabled = false;
                CancelButton.IsEnabled = true;
                DayGrid.IsEnabled = false;
                StatusLabel.Foreground = Brushes.White;
                if (!Directory.Exists(_logPath))
                    Directory.CreateDirectory(_logPath);
                _currentLogName = _logPath + DateTime.Now.ToString("yyyy-dd-M--HH-mm-ss") + ".txt";
                BackupResult result = await ExecuteBackup();
                switch (result)
                {
                    case BackupResult.Successful:
                        StatusLabel.Content = "Backup Complete";
                        StatusLabel.Foreground = Brushes.White;
                        StatusLabel.ToolTip = null;
                        break;
                    case BackupResult.Canceled:
                        StatusLabel.Content = "Backup Canceled";
                        StatusLabel.Foreground = Brushes.Yellow;
                        StatusLabel.ToolTip = "The backup process was cancled";
                        break;
                    case BackupResult.None:
                        StatusLabel.Content = "Error";
                        StatusLabel.Foreground = Brushes.Red;
                        StatusLabel.ToolTip = "An unexpected error occured during backup process! Look in the logs for details.";
                        break;
                }
                BackupProgressBar.Value = 0;
                _cancelTokenSource = new CancellationTokenSource();
                SelectFoldersButton.IsEnabled = true;
                ExcludeFoldersButton.IsEnabled = true;
                BackupNowButton.IsEnabled = true;
                CancelButton.IsEnabled = false;
                DayGrid.IsEnabled = true;
#if RELEASE
            }
#endif
        }

        private async Task<BackupResult> ExecuteBackup()
        {
            try
            {
                StatusLabel.Content = "Indexing...";
                Dictionary<BackupContext, List<FileInfo>> data = new Dictionary<BackupContext, List<FileInfo>>();
                var options = new EnumerationOptions()
                {
                    IgnoreInaccessible = true,
                    RecurseSubdirectories= true,
                };
                int maxCount = 0;
                foreach (BackupContext context in MainWindow.SaveContext.BackupContexts)
                {
                    List<FileInfo> info = await GetSizeOfDirectory(context);
                    data.Add(context, info);
                    maxCount += info.Count;
                    if (_cancelTokenSource.Token.IsCancellationRequested)
                        return BackupResult.Canceled;
                }
                BackupProgressBar.Maximum = maxCount;
                BackupProgressBar.Value = 0;

                StatusLabel.Content = "Backup Up...";
                foreach (var context in data)
                {
                    await ExecuteBackupOfContext(context);
                    if (_cancelTokenSource.Token.IsCancellationRequested)
                        return BackupResult.Canceled;
                }

                BackupProgressBar.Value = 0;

                MainWindow.SaveContext.LastBackup = DateTime.Now;
                LastBackupLabel.Content = "Last Backup: " + MainWindow.SaveContext.LastBackup;
                MainWindow.SaveContext.Save();

                return BackupResult.Successful;
            }
            catch(Exception ex) 
            {
                await File.AppendAllTextAsync(_currentLogName, ex.Message + Environment.NewLine);
                return BackupResult.None;
            }
        }

        private async Task<List<FileInfo>> GetSizeOfDirectory(BackupContext context)
        {
            if (!Directory.Exists(context.Source))
                Directory.CreateDirectory(context.Source);
            if (!Directory.Exists(context.Target))
                Directory.CreateDirectory(context.Target);

            DirectoryInfo dir1 = new DirectoryInfo(context.Source);
            var list1 = await Task.Run(() => {
                return dir1.GetFiles("*.*", _options);
            });
            return list1.ToList();
        }

        private async Task ExecuteBackupOfContext(KeyValuePair<BackupContext,List<FileInfo>> context)
        {
            foreach (var file in context.Value)
            {
                if (!IsExcluded(file.FullName))
                {
                    StatusLabel.ToolTip = "Current File: " + file;
                    FileInfo target = new FileInfo(file.FullName.Replace(context.Key.Source, context.Key.Target));

                    if (File.Exists(target.FullName))
                    {
                        if (target.LastWriteTime < file.LastWriteTime)
                            await CopyFileAsync(file.FullName, target.FullName);
                    }
                    else
                    {
                        if (!Directory.Exists(target.Directory.FullName))
                            Directory.CreateDirectory(target.Directory.FullName);
                        await CopyFileAsync(file.FullName, target.FullName);
                    }
                }
                BackupProgressBar.Value += 1;
                if (_cancelTokenSource.Token.IsCancellationRequested)
                    break;
            }
        }

        private bool IsExcluded(string fileName)
        {
            foreach (var checkFile in MainWindow.SaveContext.ExcludedFolders)
            {
                if (fileName.StartsWith(checkFile))
                {
                    return true;
                }
            }
            return false;
        }

        public async Task CopyFileAsync(string sourcePath, string destinationPath)
        {
            try
            {
                using (Stream source = File.Open(sourcePath, FileMode.Open))
                {
                    using (Stream destination = File.Create(destinationPath))
                    {
                        await source.CopyToAsync(destination, _cancelTokenSource.Token);
                    }
                }
            }
            catch (OperationCanceledException ex)
            {
                if (File.Exists(destinationPath))
                    File.Delete(destinationPath);
            }
            catch (Exception ex)
            {
                await File.AppendAllTextAsync(_currentLogName, ex.Message + Environment.NewLine);
            }
        }

        private async void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            MonCheckbox.IsChecked = MainWindow.SaveContext.BackupDays.Contains(DayOfWeek.Monday);
            TueCheckbox.IsChecked = MainWindow.SaveContext.BackupDays.Contains(DayOfWeek.Tuesday);
            WedCheckbox.IsChecked = MainWindow.SaveContext.BackupDays.Contains(DayOfWeek.Wednesday);
            ThuCheckbox.IsChecked = MainWindow.SaveContext.BackupDays.Contains(DayOfWeek.Thursday);
            FriCheckbox.IsChecked = MainWindow.SaveContext.BackupDays.Contains(DayOfWeek.Friday);
            SatCheckbox.IsChecked = MainWindow.SaveContext.BackupDays.Contains(DayOfWeek.Saturday);
            SunCheckbox.IsChecked = MainWindow.SaveContext.BackupDays.Contains(DayOfWeek.Sunday);
            LastBackupLabel.Content = "Last Backup: " + MainWindow.SaveContext.LastBackup;

            string[] args = Environment.GetCommandLineArgs();
            if (args.Contains("isAdmin") && _firstRunAfterAdmin)
            {
                await ProcessExecuteBackup();
                _firstRunAfterAdmin = false;
            }
            else
            {
                if (MainWindow.SaveContext.BackupDays.Contains(DateTime.Now.DayOfWeek))
                {
                    if (MainWindow.SaveContext.LastBackup.DayOfWeek != DateTime.Now.DayOfWeek)
                    {
                        await ProcessExecuteBackup();
                    }
                }
            }
        }

        private void ChangeBackupDay_Click(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox box)
            {
                if (box.Content is string day)
                {
                    switch (day.ToUpper())
                    {
                        case "MON": ChangeBackupDay(DayOfWeek.Monday); break;
                        case "TUE": ChangeBackupDay(DayOfWeek.Tuesday); break;
                        case "WED": ChangeBackupDay(DayOfWeek.Wednesday); break;
                        case "THU": ChangeBackupDay(DayOfWeek.Thursday); break;
                        case "FRI": ChangeBackupDay(DayOfWeek.Friday); break;
                        case "SAT": ChangeBackupDay(DayOfWeek.Saturday); break;
                        case "SUN": ChangeBackupDay(DayOfWeek.Sunday); break;
                    }
                    MainWindow.SaveContext.Save();
                }
            }
        }

        private void ChangeBackupDay(DayOfWeek day)
        {
            if (!MainWindow.SaveContext.BackupDays.Contains(day))
                MainWindow.SaveContext.BackupDays.Add(day);
            else
                MainWindow.SaveContext.BackupDays.Remove(day);
            MainWindow.SaveContext.Save();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            _cancelTokenSource.Cancel();
        }
    }
}

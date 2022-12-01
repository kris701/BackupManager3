using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
using System.Windows.Shapes;
using BackupManager3.Data;
using Nanotek.WPF.WindowsTrayItemFramework;

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
        public double TargetHeight { get; } = 160;
        private CancellationTokenSource _cancelTokenSource = new CancellationTokenSource();

        public MainView()
        {
            InitializeComponent();
            Element = this;
        }

        private void SelectFolders_Click(object sender, RoutedEventArgs e)
        {
            ViewSwitcher.SwitchView(new SelectFoldersView());
        }

        private async void BackupNow_Click(object sender, RoutedEventArgs e)
        {
            await ProcessExecuteBackup();
        }

        private async Task ProcessExecuteBackup()
        {
            SelectFoldersButton.IsEnabled = false;
            BackupNowButton.IsEnabled = false;
            DayGrid.IsEnabled = false;
            BackupResult result = await ExecuteBackup();
            switch (result)
            {
                case BackupResult.Successful:
                    BackupProgressBar.Background = Brushes.Transparent;
                    BackupProgressBar.ToolTip = "";
                    break;
                case BackupResult.Canceled:
                    BackupProgressBar.Background = Brushes.Yellow;
                    BackupProgressBar.ToolTip = "The backup process was cancled";
                    break;
                case BackupResult.None:
                    BackupProgressBar.Background = Brushes.Red;
                    BackupProgressBar.ToolTip = "An unexpected error occured during backup process!";
                    break;
            }
            _cancelTokenSource = new CancellationTokenSource();
            SelectFoldersButton.IsEnabled = true;
            BackupNowButton.IsEnabled = true;
            DayGrid.IsEnabled = true;
        }

        private async Task<BackupResult> ExecuteBackup()
        {
            int maxSteps = 0;
            foreach (BackupContext context in MainWindow.SaveContext.BackupContexts)
            {
                if (!Directory.Exists(context.Source))
                    Directory.CreateDirectory(context.Source);
                if (!Directory.Exists(context.Target))
                    Directory.CreateDirectory(context.Target);

                DirectoryInfo dir1 = new DirectoryInfo(context.Source);
                IEnumerable<FileInfo> list1 = dir1.GetFiles("*.*", SearchOption.AllDirectories);
                maxSteps += list1.Count();

                if (_cancelTokenSource.Token.IsCancellationRequested)
                    return BackupResult.Canceled;
            }
            BackupProgressBar.Maximum = maxSteps;
            BackupProgressBar.Value = 0;

            foreach (BackupContext context in MainWindow.SaveContext.BackupContexts)
            {
                DirectoryInfo dir1 = new DirectoryInfo(context.Source);
                IEnumerable<FileInfo> list1 = dir1.GetFiles("*.*", SearchOption.AllDirectories);

                foreach (var file in list1)
                {
                    string targetFileName = file.FullName.Replace(context.Source, context.Target);
                    if (File.Exists(targetFileName))
                    {
                        if (new FileInfo(targetFileName).LastWriteTime < file.LastWriteTime)
                            await CopyFileAsync(file.FullName, targetFileName);
                    }
                    else
                        await CopyFileAsync(file.FullName, targetFileName);
                    BackupProgressBar.Value += 1;
                    if (_cancelTokenSource.Token.IsCancellationRequested)
                        return BackupResult.Canceled;
                }
                if (_cancelTokenSource.Token.IsCancellationRequested)
                    return BackupResult.Canceled;
            }

            BackupProgressBar.Value = 0;

            MainWindow.SaveContext.LastBackup = DateTime.Now;
            LastBackupLabel.Content = "Last Backup: " + MainWindow.SaveContext.LastBackup;
            MainWindow.SaveContext.Save();

            return BackupResult.Successful;
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
            if (MainWindow.SaveContext.BackupDays.Contains(DateTime.Now.DayOfWeek))
            {
                if (MainWindow.SaveContext.LastBackup.DayOfWeek != DateTime.Now.DayOfWeek)
                {
                    await ProcessExecuteBackup();
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

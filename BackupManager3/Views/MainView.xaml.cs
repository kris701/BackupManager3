using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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
using BackupManager3.Services;
using Nanotek.WPF.WindowsTrayItemFramework;

namespace BackupManager3.Views
{
    /// <summary>
    /// Interaction logic for MainView.xaml
    /// </summary>
    public partial class MainView : UserControl, TrayWindowSwitchable
    {
        public UIElement Element { get; }
        public double TargetWidth { get; } = 400;
        public double TargetHeight { get; } = 160;

        public MainView()
        {
            InitializeComponent();
            Element = this;
        }

        private void SelectFolders_Click(object sender, RoutedEventArgs e)
        {

        }

        private async void BackupNow_Click(object sender, RoutedEventArgs e)
        {
            await ExecuteBackup();
        }

        private async Task ExecuteBackup()
        {
            int maxSteps = 0;
            foreach (BackupContext context in MainWindow.SaveContext.BackupContexts)
            {
                DirectoryInfo dir1 = new DirectoryInfo(context.Source);
                IEnumerable<FileInfo> list1 = dir1.GetFiles("*.*", SearchOption.AllDirectories);
                maxSteps += list1.Count();
            }
            BackupProgressBar.Maximum = maxSteps;
            BackupProgressBar.Value = 0;

            foreach (BackupContext context in MainWindow.SaveContext.BackupContexts)
            {
                DirectoryInfo dir1 = new DirectoryInfo(context.Source);

                IEnumerable<FileInfo> list1 = dir1.GetFiles("*.*", SearchOption.AllDirectories);

                foreach(var file in list1)
                {
                    string targetFileName = file.FullName.Replace(context.Source, context.Target);
                    if (File.Exists(targetFileName))
                    {
                        if (new FileInfo(targetFileName).LastWriteTime < file.LastWriteTime)
                        {
                            await CopyFileAsync(file.FullName, targetFileName);
                        }
                    }
                    else
                    {
                        await CopyFileAsync(file.FullName, targetFileName);
                    }
                    BackupProgressBar.Value += 1;
                }
            }

            BackupProgressBar.Value = 0;
        }

        public async Task CopyFileAsync(string sourcePath, string destinationPath)
        {
            using (Stream source = File.Open(sourcePath, FileMode.Open))
            {
                using (Stream destination = File.Create(destinationPath))
                {
                    await source.CopyToAsync(destination);
                }
            }
        }

        private async void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (MainWindow.SaveContext.BackupDays.Contains(DateTime.Now.DayOfWeek))
            {
                if (MainWindow.SaveContext.LastUpdate.DayOfWeek != DateTime.Now.DayOfWeek)
                {
                    await ExecuteBackup();
                }
            }
        }
    }
}

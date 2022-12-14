using BackupManager3.Data;
using BackupManager3.Views.UserControls;
using Nanotek.WPF.WindowsTrayItemFramework;
using System;
using System.Collections.Generic;
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

namespace BackupManager3.Views
{
    /// <summary>
    /// Interaction logic for ExcludeFoldersView.xaml
    /// </summary>
    public partial class ExcludeFoldersView : UserControl, TrayWindowSwitchable
    {
        public UIElement Element { get; }
        public double TargetWidth { get; } = 600;
        public double TargetHeight { get; } = 400;

        public ExcludeFoldersView()
        {
            InitializeComponent();
            Element = this;
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            List<string> newContexts = new List<string>();
            foreach (var child in FolderPanel.Children)
            {
                if (child is ExcludedFolderControl control)
                {
                    newContexts.Add(control.GetUpdatedModel());
                }
            }
            MainWindow.SaveContext.ExcludedFolders = newContexts;
            MainWindow.SaveContext.Save();
            await ViewSwitcher.SwitchView(new MainView());
        }

        private async void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            await ViewSwitcher.SwitchView(new MainView());
        }

        private void AddNewFolderButton_Click(object sender, RoutedEventArgs e)
        {
            FolderPanel.Children.Add(new ExcludedFolderControl(FolderPanel));
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            foreach (string context in MainWindow.SaveContext.ExcludedFolders)
            {
                FolderPanel.Children.Add(new ExcludedFolderControl(context, FolderPanel));
            }
        }
    }
}

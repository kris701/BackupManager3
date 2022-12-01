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

namespace BackupManager3.Views.UserControls
{
    /// <summary>
    /// Interaction logic for ExcludedFolderControl.xaml
    /// </summary>
    public partial class ExcludedFolderControl : UserControl
    {
        private Panel _parent;
        public ExcludedFolderControl(string context, Panel parent)
        {
            InitializeComponent();

            _parent = parent;
            ExcludeFolderTextbox.Text = context;
        }

        public ExcludedFolderControl(Panel parent)
        {
            InitializeComponent();

            _parent = parent;
            ExcludeFolderTextbox.Text = "";
        }

        public string GetUpdatedModel()
        {
            return ExcludeFolderTextbox.Text;
        }

        private void CheckForPathExistence_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox textbox)
            {
                if (!Directory.Exists(textbox.Text))
                {
                    textbox.Background = Brushes.Red;
                    textbox.ToolTip = "Directory not found!";
                }
                else
                {
                    textbox.Background = Brushes.Green;
                    textbox.ToolTip = null;
                }
            }
        }

        private void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
            _parent.Children.Remove(this);
        }
    }
}

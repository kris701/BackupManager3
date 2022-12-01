﻿using BackupManager3.Data;
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
    /// Interaction logic for BackupContextControl.xaml
    /// </summary>
    public partial class BackupContextControl : UserControl
    {
        public BackupContextControl(BackupContext context)
        {
            InitializeComponent();

            SourceFolderTextbox.Text = context.Source;
            TargetFolderTextbox.Text = context.Target;
        }
        public BackupContextControl()
        {
            InitializeComponent();

            SourceFolderTextbox.Text = "";
            TargetFolderTextbox.Text = "";
        }

        public BackupContext GetUpdatedModel()
        {
            return new BackupContext(SourceFolderTextbox.Text, TargetFolderTextbox.Text);
        }

        private void CheckForPathExistence_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox textbox)
            {
                if (!Directory.Exists(textbox.Text))
                {
                    textbox.Background = Brushes.Red;
                    textbox.ToolTip = "Directory not found! One will be created upon first backup.";
                }
                else
                {
                    textbox.Background = Brushes.Green;
                    textbox.ToolTip = null;
                }
            }
        }
    }
}

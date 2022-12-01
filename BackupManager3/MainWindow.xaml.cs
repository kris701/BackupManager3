using BackupManager3.Data;
using BackupManager3.Views;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
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
using Nanotek.WPF.WindowsTrayItemFramework;

namespace BackupManager3
{
    public partial class MainWindow : TrayWindow
    {
        public static SaveModel SaveContext { get; internal set; }

        public MainWindow() : base("Backup Manager 3", GetEmbeddedIcon(), new MainView())
        {
            if (File.Exists("save.json"))
            {
                var result = JsonSerializer.Deserialize<SaveModel>(File.ReadAllText("save.json"));
                if (result != null)
                    SaveContext = result;
                else
                    SaveContext = new SaveModel();
            }
            else
                SaveContext = new SaveModel();
            InitializeComponent();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Save();
        }

        private void Save()
        {
            if (File.Exists("save.json"))
                File.Delete("save.json");
            File.WriteAllText("save.json", JsonSerializer.Serialize(SaveContext));
        }

        private static System.Drawing.Icon GetEmbeddedIcon()
        {
            System.Reflection.Assembly a = System.Reflection.Assembly.GetExecutingAssembly();
            var st = a.GetManifestResourceStream("BackupManager3.icon.ico");
            if (st == null)
                throw new FileNotFoundException("Error! Icon file not found!");
            return new System.Drawing.Icon(st);
        }
    }
}

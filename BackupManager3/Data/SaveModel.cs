using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace BackupManager3.Data
{
    public class SaveModel
    {
        public List<DayOfWeek> BackupDays { get; set; }
        public DateTime LastBackup { get; set; }
        public List<BackupContext> BackupContexts { get; set; }
        public List<string> ExcludedFolders { get; set; }

        public SaveModel(List<DayOfWeek> backupDays, DateTime lastBackup, List<BackupContext> backupContexts, List<string> excludedFolders)
        {
            BackupDays = backupDays;
            LastBackup = lastBackup;
            BackupContexts = backupContexts;
            ExcludedFolders = new List<string>();
            foreach (var folder in excludedFolders) {
                if (!folder.EndsWith("\\"))
                    ExcludedFolders.Add(folder + "\\");
                else
                    ExcludedFolders.Add(folder);
            }
        }

        public SaveModel()
        {
            BackupDays = new List<DayOfWeek>();
            LastBackup = DateTime.Now;
            BackupContexts = new List<BackupContext>();
            ExcludedFolders = new List<string>();
        }

        public void Load()
        {
            if (File.Exists("save.json"))
            {
                var result = JsonSerializer.Deserialize<SaveModel>(File.ReadAllText("save.json"));
                if (result != null)
                {
                    BackupDays = result.BackupDays;
                    LastBackup = result.LastBackup;
                    BackupContexts = result.BackupContexts;
                    ExcludedFolders = result.ExcludedFolders;
                }
                else
                {

                    BackupDays = new List<DayOfWeek>();
                    LastBackup = DateTime.Now;
                    BackupContexts = new List<BackupContext>();
                    ExcludedFolders = new List<string>();
                }
            }
            else
            {

                BackupDays = new List<DayOfWeek>();
                LastBackup = DateTime.Now;
                BackupContexts = new List<BackupContext>();
                ExcludedFolders = new List<string>();
            }
        }

        public void Save()
        {
            if (File.Exists("save.json"))
                File.Delete("save.json");
            File.WriteAllText("save.json", JsonSerializer.Serialize(this));
        }
    }
}

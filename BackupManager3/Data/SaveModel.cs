using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackupManager3.Data
{
    public class SaveModel
    {
        public List<DayOfWeek> BackupDays { get; set; }
        public DateTime LastUpdate { get; set; }
        public List<BackupContext> BackupContexts { get; set; }

        public SaveModel(List<DayOfWeek> backupDays, DateTime lastUpdate, List<BackupContext> backupContexts)
        {
            BackupDays = backupDays;
            LastUpdate = lastUpdate;
            BackupContexts = backupContexts;
        }

        public SaveModel()
        {
            BackupDays = new List<DayOfWeek>();
            LastUpdate = DateTime.Now;
            BackupContexts = new List<BackupContext>();
        }
    }
}

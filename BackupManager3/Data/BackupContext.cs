using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackupManager3.Data
{
    public class BackupContext
    {
        public string Source { get; set; }
        public string Target { get; set; }

        public BackupContext(string source, string target)
        {
            Source = source;
            Target = target;
        }
    }
}

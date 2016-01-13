using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace SandboxDatabaseManager.Models
{
    public class BackupDatabaseDetails
    {
        public string SourceDatabaseServer { get; set; }

        public string SourceDatabaseName { get; set; }

        public decimal SourceDatabaseSizeGB { get; set; }

        [Required]
        public string BackupServer { get; set; }
        
        [Required]
        public string BackupFileName { get; set; }
        public string BackupDestinationPath { get; set; }

        public bool? IsOverride { get; set; }

        [MaxLength(255)]
        public string BackupComment { get; set; }
    }
}
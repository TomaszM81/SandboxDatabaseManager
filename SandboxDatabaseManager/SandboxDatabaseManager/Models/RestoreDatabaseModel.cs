using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace SandboxDatabaseManager.Models
{
    public class RestoreDatabaseModel
    {
        public string LocationName { get; set; }
        public string BackupServerName { get; set; }

        public string BackupDatabaseName { get; set; }

        public decimal BackupDatabaseSizeGB { get; set; }

        public string SQLServerVersion { get; set; }

        public string RecoveryModel { get; set; }

        public string BackupFileList { get; set; }

        public string TargetDatabaseServer { get; set; }

        public string LastWarningMessage { get; set; }

        public string BackupType { get; set; }
        public string FirstLSN { get; set; }
        public string LastLSN { get; set; }
        public string CheckpointLSN { get; set; }
        public string DatabaseBackupLSN { get; set; }
        public int PositinInFileCollection { get; set; }
        public string BackupDate { get; set; }


        [Required]
        [Display(Name = "Target Database Name")]
        [MaxLength(100)]
        public string TargetDatabaseName { get; set; }
       
        [MaxLength(255)]
        public string DatabaseComment { get; set; }
        public bool RecoveryModelChangeToSimple { get; set; }
        public bool RestoreWithRecovery { get; set; }
    }
}
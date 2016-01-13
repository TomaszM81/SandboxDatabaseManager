using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace SandboxDatabaseManager.Models
{
    public class CopyDatabaseDetails
    {
        public string SourceDatabaseServer { get; set; }
        public string SourceDatabaseName { get; set; }
        public decimal SourceDatabaseSizeGB { get; set; }
        public string SourceDatabaseRecoveryModel { get; set; }
        public string TargetDatabaseServer { get; set; }
        [Required]
        [Display(Name = "Target Database Name")]
        [MaxLength(100)]    
        public string TargetDatabaseName { get; set; }
        public string LastWarningMessage { get; set; } 
        [MaxLength(255)]
        public string DatabaseComment { get; set; }
        public bool RecoveryModelChangeToSimple { get; set; }

    }
}
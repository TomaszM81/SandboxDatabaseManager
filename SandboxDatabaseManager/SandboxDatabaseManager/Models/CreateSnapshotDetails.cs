using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace SandboxDatabaseManager.Models
{
    public class CreateSnapshotDetails
    {
        public string DatabaseServer { get; set; }
        public string DatabaseName { get; set; }

        [Required]
        [Display(Name = "Database Snapshot Name")]
        [MaxLength(100)]
        public string DatabaseSnapshotName { get; set; }
        public string LastWarningMessage { get; set; }

    }
}
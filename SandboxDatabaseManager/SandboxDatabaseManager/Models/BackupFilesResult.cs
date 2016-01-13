using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web;

namespace SandboxDatabaseManager.Models
{
    public class BackupFilesResult
    {
        public DataTable BackupFiles { get; set; }
        public string InfoMessage { get; set; }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SandboxDatabaseManager.Database
{
    public class ValidateRestoreOperationResult
    {
        public bool? @CanOverwrite { get; set; }
        public string CustomErrorMessage { get; set; }
    }
}
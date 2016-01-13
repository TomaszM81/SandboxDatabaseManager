using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace SandboxDatabaseManager.Models
{
    public class TransferOwnershipData
    {
        public string DatabaseServer { get; set; }
        public string DatabaseName { get; set; }
        public string NewDatabaseOwner { get; set; }

    }
}
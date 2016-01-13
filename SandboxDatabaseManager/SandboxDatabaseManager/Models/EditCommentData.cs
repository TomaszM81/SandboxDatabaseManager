using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace SandboxDatabaseManager.Models
{
    public class EditCommentData
    {
        public string DatabaseServer { get; set; }
        public string DatabaseName { get; set; }
        public string DatabaseComment { get; set; }

    }
}
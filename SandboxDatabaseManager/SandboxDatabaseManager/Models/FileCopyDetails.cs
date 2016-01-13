using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace SandboxDatabaseManager.Models
{
    public class FileCopyDetails
    {

        [Required]
        public string SourceLocation { get; set; }
        [Required]
        public string DestinationLocation { get; set; }
        [Required]
        public string SourceSubPath { get; set; }
    }
}
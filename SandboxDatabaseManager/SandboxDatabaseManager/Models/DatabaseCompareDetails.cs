using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace SandboxDatabaseManager.Models
{
    public class DatabaseCompareDetails
    {
        public string DatabaseServer { get; set; }

        [Required(ErrorMessage = "Please specify the database you wish to compare")]
        [MaxLength(100)]
        [Display(Name = "Database Name To Compare")]
        public string DatabaseNameToCompare { get; set; }

        [Required(ErrorMessage = "Please specify the database you wish to compare against")]
        [MaxLength(100)]
        [Display(Name = "Database Name To Compare Against")]
        public string DatabaseNameToCompareAgaints { get; set; }

        public string ListOfTablesToCompare { get; set; }

        public int? MaxTableRowCount { get; set; }

    }
}
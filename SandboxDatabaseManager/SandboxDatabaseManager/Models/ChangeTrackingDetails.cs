using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Linq;
using System.Web;

namespace SandboxDatabaseManager.Models
{
    public class ChangeTrackingDetails
    {
        public string DatabaseServer { get; set; }
        public string DatabaseName { get; set; }
        public string Owner { get; set; }
        public int? ShowChangesFromRevision{ get; set; }
        public string TargetDatabaseServer { get; set; }
        public string ListOfTablesToCompare { get; set; }
        public DataTable TrackDataChangesReslut { get; set; }
        public string TrackDataChangesLog { get; set; }
    }
}
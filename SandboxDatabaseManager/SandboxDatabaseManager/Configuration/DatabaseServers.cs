using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;

namespace SandboxDatabaseManager.Configuration
{
    public class DatabaseServers : System.Configuration.ConfigurationSection
    {
        private const string _sectionName = "DatabaseServersList";

        [ConfigurationProperty("", IsRequired = false, IsKey = false, IsDefaultCollection = true)]
        public DatabaseServersItemCollection Items
        {
            get
            {

                var name = this;

                return ((DatabaseServersItemCollection)(base[""]));


            }
            set { base[""] = value; }
        }

        private static DatabaseServers _instance;
        public static DatabaseServers Instance
        {
            get
            {
                if (_instance != null)
                    return _instance;

                _instance = (DatabaseServers)ConfigurationManager.GetSection(_sectionName);

                return _instance;
            }

        }

        public List<DatabaseServerInfo> ItemsList
        {
            get
            {
                List<DatabaseServerInfo> list = new List<DatabaseServerInfo>();

                foreach (var server in Items)
                {
                    DatabaseServerInfo srvInfo = (DatabaseServerInfo)server;
                    list.Add(srvInfo);
                }

                return list;


            }

        }




        public String GetConnectionString(string serverName)
        {



            foreach (var server in Items)
            {
                DatabaseServerInfo srvInfo = (DatabaseServerInfo)server;
                if (srvInfo.Name == serverName)
                    return srvInfo.ConnectionString;
            }


            return null;
        }
    }

    [ConfigurationCollection(typeof(DatabaseServerInfo), CollectionType = ConfigurationElementCollectionType.BasicMapAlternate)]
    public class DatabaseServersItemCollection : ConfigurationElementCollection
    {
        internal const string ItemPropertyName = "Server";

        public DatabaseServerInfo this[int index]
        {
            get { return (DatabaseServerInfo)BaseGet(index); }
            set
            {
                if (BaseGet(index) != null)
                {
                    BaseRemoveAt(index);
                }
                BaseAdd(index, value);
            }
        }

        public void Add(DatabaseServerInfo mapping)
        {
            BaseAdd(mapping);
        }

        public void Clear()
        {
            BaseClear();
        }



        public override ConfigurationElementCollectionType CollectionType
        {
            get { return ConfigurationElementCollectionType.BasicMapAlternate; }
        }

        protected override string ElementName
        {
            get { return ItemPropertyName; }
        }

        protected override bool IsElementName(string elementName)
        {
            return (elementName == ItemPropertyName);
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((DatabaseServerInfo)element).Name;
        }

        protected override ConfigurationElement CreateNewElement()
        {
            return new DatabaseServerInfo();
        }

        public override bool IsReadOnly()
        {
            return false;
        }

    }

    public class DatabaseServerInfo : ConfigurationElement
    {
        [ConfigurationProperty("Name", IsKey = true, IsRequired = true)]
        public string Name
        {
            get { return (string)base["Name"]; }
            set { base["Name"] = value; }
        }

        [ConfigurationProperty("ConnectionString", IsRequired = true)]
        public string ConnectionString
        {
            get { return (string)base["ConnectionString"]; }
            set { base["ConnectionString"] = value; }
        }

        [ConfigurationProperty("CopyDatabaseNetworkSharePath", IsRequired = true)]
        public string CopyDatabaseNetworkSharePath
        {
            get 
            {
                var value = (string)base["CopyDatabaseNetworkSharePath"];
                if(!String.IsNullOrWhiteSpace(value))
                {
                    value = value.Trim();

                    if (!value.EndsWith(@"\"))
                        value += @"\";
                }

                return value;
            }
            set { base["CopyDatabaseNetworkSharePath"] = value; }
        }

        /// <summary>
        /// This will provide a path for a database backup file if this server is choosen as a destination
        /// </summary>
        [ConfigurationProperty("BackupDatabaseNetworkSharePath", IsRequired = true)]
        public string BackupDatabaseNetworkSharePath
        {
            get
            {
                var value = (string)base["BackupDatabaseNetworkSharePath"];
                if (!String.IsNullOrWhiteSpace(value))
                {
                    value = value.Trim();

                    if (!value.EndsWith(@"\"))
                        value += @"\";
                }

                return value;
            }
            set { base["CopyDatabaseNetworkSharePath"] = value; }
        }

        [ConfigurationProperty("IsPrimary", IsKey = true, IsRequired = true)]
        public bool IsPrimary
        {
            get { return (bool)base["IsPrimary"]; }
            set { base["IsPrimary"] = value; }
        }

        [ConfigurationProperty("UseForBackupFileScan", IsKey = false, IsRequired = false)]
        public bool UseForBackupFileScan
        {
            get { return (bool)base["UseForBackupFileScan"]; }
            set { base["UseForBackupFileScan"] = value; }
        }

        [ConfigurationProperty("MonitoredServerFriendlyNameForFreeSpace", IsKey = false, IsRequired = false)]
        public string MonitoredServerFriendlyNameForFreeSpace
        {
            get { return (string)base["MonitoredServerFriendlyNameForFreeSpace"]; }
            set { base["MonitoredServerFriendlyNameForFreeSpace"] = value; }
        }

        [ConfigurationProperty("MonitoredServerCounterFriendlyNameForFreeSpace", IsKey = false, IsRequired = false)]
        public string MonitoredServerCounterFriendlyNameForFreeSpace
        {
            get { return (string)base["MonitoredServerCounterFriendlyNameForFreeSpace"]; }
            set { base["MonitoredServerCounterFriendlyNameForFreeSpace"] = value; }
        }
    }
}
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;

namespace SandboxDatabaseManager.Configuration
{
    public class DatabaseBackupFileLocations : System.Configuration.ConfigurationSection
    {
        private const string _sectionName = "DatabaseBackupFileLocationsList";

        [ConfigurationProperty("", IsRequired = false, IsKey = false, IsDefaultCollection = true)]
        public DatabaseBackupFileLocationsItemCollection Items
        {
            get
            {

                var name = this;

                return ((DatabaseBackupFileLocationsItemCollection)(base[""]));


            }
            set { base[""] = value; }
        }

        private static DatabaseBackupFileLocations _instance;
        public static DatabaseBackupFileLocations Instance
        {
            get
            {
                if (_instance != null)
                    return _instance;

                _instance = (DatabaseBackupFileLocations)ConfigurationManager.GetSection(_sectionName);

                return _instance;
            }

        }

        public List<DatabaseBackupFileLocation> ItemsList
        {
            get
            {
                List<DatabaseBackupFileLocation> list = new List<DatabaseBackupFileLocation>();

                foreach (var server in Items)
                {
                    DatabaseBackupFileLocation srvInfo = (DatabaseBackupFileLocation)server;
                    list.Add(srvInfo);
                }

                return list;


            }

        }




        public String GetPathByName(string name)
        {

            foreach (var server in Items)
            {
                DatabaseBackupFileLocation srvInfo = (DatabaseBackupFileLocation)server;
                if (srvInfo.Name == name)
                    return srvInfo.Path;
            }


            return null;
        }
    }

    [ConfigurationCollection(typeof(MonitoredServerInfo), CollectionType = ConfigurationElementCollectionType.BasicMapAlternate)]
    public class DatabaseBackupFileLocationsItemCollection : ConfigurationElementCollection
    {
        internal const string ItemPropertyName = "FileLocation";

        public DatabaseBackupFileLocation this[int index]
        {
            get { return (DatabaseBackupFileLocation)BaseGet(index); }
            set
            {
                if (BaseGet(index) != null)
                {
                    BaseRemoveAt(index);
                }
                BaseAdd(index, value);
            }
        }

        public void Add(DatabaseBackupFileLocation mapping)
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
            return ((DatabaseBackupFileLocation)element).Name;
        }

        protected override ConfigurationElement CreateNewElement()
        {
            return new DatabaseBackupFileLocation();
        }

        public override bool IsReadOnly()
        {
            return false;
        }

    }

    public class DatabaseBackupFileLocation : ConfigurationElement
    {
        [ConfigurationProperty("Name", IsKey = true, IsRequired = true)]
        public string Name
        {
            get { return (string)base["Name"]; }
            set { base["Name"] = value; }
        }

        [ConfigurationProperty("Path", IsRequired = true)]
        public string Path
        {
            get { return (string)base["Path"]; }
            set { base["Path"] = value; }
        }
    } 
}
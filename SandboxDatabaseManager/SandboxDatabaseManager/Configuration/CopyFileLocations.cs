using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;

namespace SandboxDatabaseManager.Configuration
{
    public class CopyFileLocations : System.Configuration.ConfigurationSection
    {
        private const string _sectionName = "CopyFileLocationsList";

        [ConfigurationProperty("", IsRequired = false, IsKey = false, IsDefaultCollection = true)]
        public CopyFileLocationsItemCollection Items
        {
            get
            {

                var name = this;

                return ((CopyFileLocationsItemCollection)(base[""]));


            }
            set { base[""] = value; }
        }

        private static CopyFileLocations _instance;
        public static CopyFileLocations Instance
        {
            get
            {
                if (_instance != null)
                    return _instance;

                _instance = (CopyFileLocations)ConfigurationManager.GetSection(_sectionName);

                return _instance;
            }

        }

        public List<CopyFileLocation> ItemsList
        {
            get
            {
                List<CopyFileLocation> list = new List<CopyFileLocation>();

                foreach (var server in Items)
                {
                    CopyFileLocation srvInfo = (CopyFileLocation)server;
                    list.Add(srvInfo);
                }

                return list;


            }

        }




        public String GetPathByName(string name)
        {

            foreach (var server in Items)
            {
                CopyFileLocation srvInfo = (CopyFileLocation)server;
                if (srvInfo.Name == name)
                    return srvInfo.Path;
            }


            return null;
        }
    }

    [ConfigurationCollection(typeof(MonitoredServerInfo), CollectionType = ConfigurationElementCollectionType.BasicMapAlternate)]
    public class CopyFileLocationsItemCollection : ConfigurationElementCollection
    {
        internal const string ItemPropertyName = "CopyFileLocation";

        public MonitoredServerInfo this[int index]
        {
            get { return (MonitoredServerInfo)BaseGet(index); }
            set
            {
                if (BaseGet(index) != null)
                {
                    BaseRemoveAt(index);
                }
                BaseAdd(index, value);
            }
        }

        public void Add(CopyFileLocation mapping)
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
            return ((CopyFileLocation)element).Name;
        }

        protected override ConfigurationElement CreateNewElement()
        {
            return new CopyFileLocation();
        }

        public override bool IsReadOnly()
        {
            return false;
        }

    }

    public class CopyFileLocation : ConfigurationElement
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
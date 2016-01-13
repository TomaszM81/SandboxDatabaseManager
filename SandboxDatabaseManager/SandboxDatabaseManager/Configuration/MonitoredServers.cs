using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;

namespace SandboxDatabaseManager.Configuration
{
    public class MonitoredServers : System.Configuration.ConfigurationSection
    {
        private const string _sectionName = "MonitoredServersList";

        [ConfigurationProperty("", IsRequired = false, IsKey = false, IsDefaultCollection = true)]
        public MonitoredServersItemCollection Items
        {
            get
            {

                var name = this;

                return ((MonitoredServersItemCollection)(base[""]));


            }
            set { base[""] = value; }
        }

        private static MonitoredServers _instance;
        public static MonitoredServers Instance
        {
            get
            {
                if (_instance != null)
                    return _instance;

                _instance = (MonitoredServers)ConfigurationManager.GetSection(_sectionName);

                return _instance;
            }

        }

        public List<MonitoredServerInfo> ItemsList
        {
            get
            {
                List<MonitoredServerInfo> list = new List<MonitoredServerInfo>();

                foreach (var server in Items)
                {
                    MonitoredServerInfo srvInfo = (MonitoredServerInfo)server;
                    list.Add(srvInfo);
                }

                return list;


            }

        }




        public String GetRemoteAddress(string FriendlyName)
        {



            foreach (var server in Items)
            {
                MonitoredServerInfo srvInfo = (MonitoredServerInfo)server;
                if (srvInfo.FriendlyName == FriendlyName)
                    return srvInfo.RemoteAddress;
            }


            return null;
        }
    }

    [ConfigurationCollection(typeof(MonitoredServerInfo), CollectionType = ConfigurationElementCollectionType.BasicMapAlternate)]
    public class MonitoredServersItemCollection : ConfigurationElementCollection
    {
        internal const string ItemPropertyName = "Server";

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

        public void Add(MonitoredServerInfo mapping)
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
            return ((MonitoredServerInfo)element).FriendlyName;
        }

        protected override ConfigurationElement CreateNewElement()
        {
            return new MonitoredServerInfo();
        }

        public override bool IsReadOnly()
        {
            return false;
        }

    }

    public class MonitoredServerInfo : ConfigurationElement
    {
        [ConfigurationProperty("FriendlyName", IsKey = true, IsRequired = true)]
        public string FriendlyName
        {
            get { return (string)base["FriendlyName"]; }
            set { base["FriendlyName"] = value; }
        }

        [ConfigurationProperty("RemoteAddress", IsRequired = true)]
        public string RemoteAddress
        {
            get { return (string)base["RemoteAddress"]; }
            set { base["RemoteAddress"] = value; }
        }
    } 
}
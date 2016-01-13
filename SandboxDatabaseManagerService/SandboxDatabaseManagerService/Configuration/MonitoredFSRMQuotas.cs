using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;


namespace SandboxDatabaseManagerService.Configuration
{
    public class MonitoredFSRMQuotasSection : System.Configuration.ConfigurationSection
    {
        private const string _sectionName = "MonitoredFSRMQuotaList";

        [ConfigurationProperty("", IsRequired = false, IsKey = false, IsDefaultCollection = true)]
        public MonitoredFSRMQuotasItemCollection Items
        {
            get
            {

                var name = this;

                return ((MonitoredFSRMQuotasItemCollection)(base[""]));


            }
            set { base[""] = value; }
        }

        private static MonitoredFSRMQuotasSection _instance;
        public static MonitoredFSRMQuotasSection Instance
        {
            get
            {
                if (_instance != null)
                    return _instance;

                _instance = (MonitoredFSRMQuotasSection)ConfigurationManager.GetSection(_sectionName);

                return _instance;
            }

        }

        public List<MonitoredFSRMQuota> ItemsList
        {
            get
            {
                List<MonitoredFSRMQuota> list = new List<MonitoredFSRMQuota>();

                foreach (var server in Items)
                {
                    MonitoredFSRMQuota srvInfo = (MonitoredFSRMQuota)server;
                    list.Add(srvInfo);
                }

                return list;


            }

        }



    }

    [ConfigurationCollection(typeof(MonitoredFSRMQuota), CollectionType = ConfigurationElementCollectionType.BasicMapAlternate)]
    public class MonitoredFSRMQuotasItemCollection : ConfigurationElementCollection
    {
        internal const string ItemPropertyName = "MonitoredFSRMQuota";

        public MonitoredFSRMQuota this[int index]
        {
            get { return (MonitoredFSRMQuota)BaseGet(index); }
            set
            {
                if (BaseGet(index) != null)
                {
                    BaseRemoveAt(index);
                }
                BaseAdd(index, value);
            }
        }

        public void Add(MonitoredFSRMQuota mapping)
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

        protected override ConfigurationElement CreateNewElement()
        {
            return new MonitoredFSRMQuota();
        }

        public override bool IsReadOnly()
        {
            return false;
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return String.Format("{0}", ((MonitoredFSRMQuota)element).FriendlyName);
        }


    }

    public class MonitoredFSRMQuota : ConfigurationElement
    {

        [ConfigurationProperty("FriendlyName", IsRequired = true, IsKey = true)]
        public string FriendlyName
        {
            get { return (string)base["FriendlyName"]; }
            set { base["FriendlyName"] = value; }
        }

        [ConfigurationProperty("DotNetFormatString", IsRequired = true)]
        public string DotNetFormatString
        {
            get { return (string)base["DotNetFormatString"]; }
            set { base["DotNetFormatString"] = value; }
        }

        [ConfigurationProperty("ChartYAxisSufix", IsRequired = true)]
        public string ChartYAxisSufix
        {
            get { return (string)base["ChartYAxisSufix"]; }
            set { base["ChartYAxisSufix"] = value; }
        }

        [ConfigurationProperty("DivideRawValueBy", IsRequired = true)]
        public string DivideRawValueBy
        {
            get { return (string)base["DivideRawValueBy"]; }
            set { base["DivideRawValueBy"] = value; }
        }

        [ConfigurationProperty("FSRMQuotaFolder", IsRequired = true)]
        public string QuotaFolder
        {
            get { return (string)base["FSRMQuotaFolder"]; }
            set { base["FSRMQuotaFolder"] = value; }
        }

        [ConfigurationProperty("LowWarningValue", IsRequired = true)]
        public string LowWarningValue
        {
            get { return (string)base["LowWarningValue"]; }
            set { base["LowWarningValue"] = value; }
        }

        [ConfigurationProperty("HighWarningValue", IsRequired = true)]
        public string HighWarningValue
        {
            get { return (string)base["HighWarningValue"]; }
            set { base["HighWarningValue"] = value; }
        }

        

    } 
}
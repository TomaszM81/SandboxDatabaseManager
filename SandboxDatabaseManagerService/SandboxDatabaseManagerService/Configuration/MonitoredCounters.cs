using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;


namespace SandboxDatabaseManagerService.Configuration
{
    public class MonitoredCountersSection : System.Configuration.ConfigurationSection
    {
        private const string _sectionName = "MonitoredCountersList";

        [ConfigurationProperty("", IsRequired = false, IsKey = false, IsDefaultCollection = true)]
        public ItemCollection Items
        {
            get
            {

                var name = this;

                return ((ItemCollection)(base[""]));


            }
            set { base[""] = value; }
        }

        private static MonitoredCountersSection _instance;
        public static MonitoredCountersSection Instance
        {
            get
            {
                if (_instance != null)
                    return _instance;

                _instance = (MonitoredCountersSection)ConfigurationManager.GetSection(_sectionName);

                return _instance;
            }

        }

        public List<MonitoredCounter> ItemsList
        {
            get
            {
                List<MonitoredCounter> list = new List<MonitoredCounter>();

                foreach (var server in Items)
                {
                    MonitoredCounter srvInfo = (MonitoredCounter)server;
                    list.Add(srvInfo);
                }

                return list;


            }

        }



    }

    [ConfigurationCollection(typeof(MonitoredCounter), CollectionType = ConfigurationElementCollectionType.BasicMapAlternate)]
    public class ItemCollection : ConfigurationElementCollection
    {
        internal const string ItemPropertyName = "Counter";

        public MonitoredCounter this[int index]
        {
            get { return (MonitoredCounter)BaseGet(index); }
            set
            {
                if (BaseGet(index) != null)
                {
                    BaseRemoveAt(index);
                }
                BaseAdd(index, value);
            }
        }

        public void Add(MonitoredCounter mapping)
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
            return new MonitoredCounter();
        }

        public override bool IsReadOnly()
        {
            return false;
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return String.Format("{0}.{1}.{2}", ((MonitoredCounter)element).CategoryName, ((MonitoredCounter)element).CounterName, ((MonitoredCounter)element).InstanceName);
        }


    }

    public class MonitoredCounter : ConfigurationElement
    {
        [ConfigurationProperty("CategoryName", IsRequired = true)]
        public string CategoryName
        {
            get { return (string)base["CategoryName"]; }
            set { base["CategoryName"] = value; }
        }

        [ConfigurationProperty("CounterName", IsRequired = true)]
        public string CounterName
        {
            get { return (string)base["CounterName"]; }
            set { base["CounterName"] = value; }
        }

        [ConfigurationProperty("InstanceName", IsRequired = true)]
        public string InstanceName
        {
            get { return (string)base["InstanceName"]; }
            set { base["InstanceName"] = value; }
        }

        [ConfigurationProperty("FriendlyName", IsRequired = true , IsKey = true)]
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

        [ConfigurationProperty("DivideRawCounterValueBy", IsRequired = true)]
        public string DivideRawCounterValueBy
        {
            get { return (string)base["DivideRawCounterValueBy"]; }
            set { base["DivideRawCounterValueBy"] = value; }
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
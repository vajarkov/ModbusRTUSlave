using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;

namespace ModbusRTUService
{
    public class SerialPortSection: ConfigurationSection
    {
        [ConfigurationProperty("SerialPortSettings", IsRequired=true)]
        public SerialPortSettings SerialSettings
        {
            get { return ((SerialPortSettings)(base["SerialPortSettings"]));}
        }

        

    }

    [ConfigurationCollection(typeof(Settings))]
    public class SerialPortSettings : ConfigurationElementCollection
    {
        protected override ConfigurationElement CreateNewElement()
        {
            return new Settings();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((Settings)(element)).Name;
        }

        public Settings this[int idx]
        {
            get { return (Settings) BaseGet(idx); }
        }

        public Settings this[string name]
        {
            get { return (Settings)BaseGet((Object)name); }
        }
    }

    public class Settings : ConfigurationElement
    {
        [ConfigurationProperty("name", DefaultValue = "", IsKey = true, IsRequired = true)]
        public string Name
        {
            get { return ((string)(base["name"])); }
            set { base["name"] = value; }
        }

        [ConfigurationProperty("value", DefaultValue = "", IsKey = false, IsRequired = true)]
        public string Value
        {
            get { return ((string)(base["value"])); }
            set { base["value"] = value; }
        }


    }
}

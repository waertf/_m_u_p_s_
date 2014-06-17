using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace ConsoleApplication1_client_threading
{
     class SqlClass
    {
        public string XmlRootTag;
        public Hashtable Htable;
        public List<string> SensorName;
        public List<string> SensorType;
        public List<string> SensorValue;
        public HashSet<XName> Elements;
        public string Log1;
        public string GetMessage;

        public SqlClass(string xml_root_tag, Hashtable htable, List<string> sensor_name,
            List<string> sensor_type, List<string> sensor_value, HashSet<XName> elements, string log1,
            string getMessage)
        {
            XmlRootTag = xml_root_tag;
            Htable = new Hashtable(htable);
            //SensorName = new List<string>(sensor_name);
            //SensorType = new List<string>(sensor_type);
            //SensorValue = new List<string>(sensor_value);
            Elements = elements;
            Log1 = log1;
            GetMessage = getMessage;
        }
        ~SqlClass()
        {
            XmlRootTag = Log1 = GetMessage = null;
            Htable = null;
            SensorName = null;
            SensorType = null;
            SensorValue = null;
            Elements = null;
        }
    }
}

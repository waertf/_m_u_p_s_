using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace ConsoleApplication1_client_threading
{
    class AvlsClass
    {
        public string XmlRootTag;
        public Hashtable Htable;
        public List<string> SensorName;
        public List<string> SensorType;
        public List<string> SensorValue;
        public HashSet<XName> Elements;
        public string Log;
        public string GetMessage;

        public AvlsClass(string xml_root_tag, Hashtable htable, List<string> sensor_name,
            List<string> sensor_type, List<string> sensor_value, HashSet<XName> iEnumerable, string log,
             string getMessage)
        {
            XmlRootTag = xml_root_tag;
            Htable = new Hashtable(htable);
            //SensorName = new List<string>(sensor_name);
            //SensorType = new List<string>(sensor_type);
            //SensorValue = new List<string>(sensor_value);
            Elements = iEnumerable;
            Log = log;
            GetMessage = getMessage;
        }
        ~AvlsClass()
        {
            XmlRootTag = Log = GetMessage = null;
            Htable = null;
            SensorName = null;
            SensorType = null;
            SensorValue = null;
            Elements = null;
        }
    }
}

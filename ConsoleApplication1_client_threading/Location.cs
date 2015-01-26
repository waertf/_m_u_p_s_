using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ConsoleApplication1_client_threading
{
    class Location
    {
        private string lat { get; set; }
        private string lon { get; set; }

        public string GetLon()
        {
            return lon;
        }

        public string GetLat()
        {
            return lat;
        }

        public void SetLon(string tmplon)
        {
            lon = tmplon;
        }

        public void SetLat(string tmplat)
        {
            lat = tmplat;
        }
    }
}

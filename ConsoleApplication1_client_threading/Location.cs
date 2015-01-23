using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ConsoleApplication1_client_threading
{
    class Location
    {
        private double lat { get; set; }
        private double lon { get; set; }

        public double GetLon()
        {
            return lon;
        }

        public double GetLat()
        {
            return lat;
        }

        public void SetLon(double tmplon)
        {
            lon = tmplon;
        }

        public void SetLat(double tmplat)
        {
            lat = tmplat;
        }
    }
}

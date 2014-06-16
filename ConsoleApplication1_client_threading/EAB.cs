using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ConsoleApplication1_client_threading
{
    class EAB //exclusion_area_boundary
    {
        public string rid { get; set; }
        public string the_geom { get; set; }

        public EAB(string Rid, string Geom)
        {
            rid = Rid;
            the_geom = Geom;
        }
    }
}

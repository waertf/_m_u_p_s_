using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ConsoleApplication1_client_threading
{
    class EAB2 //exclusion_area_boundary
    {
        public string table { get; set; }
        public string gid { get; set; }
        public string fullname { get; set; }

        public EAB2(string Table, string Gid, string Fullname)
        {
            table = Table;
            gid = Gid;
            fullname = Fullname;
        }
    }
}

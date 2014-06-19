using System;
using System.Collections.Generic;
using System.Data.Linq;
using System.Linq;
using System.Text;

namespace ConsoleApplication1_client_threading
{
    public static class SqlCeCompiledQuery
    {
        /*
        public static Func<StayCheck, string, IQueryable<string>> searchID1 =
        CompiledQuery.Compile((StayCheck db, string id) =>
            from c in db.CheckIfOverTime where c.Uid == id select c.Uid);
        public static Func<StayCheck, string, IQueryable<string>> searchID2 =
        CompiledQuery.Compile((StayCheck db, string id) =>
            from c in db.CheckIfOverTime2 where c.Uid == id select c.Uid);
        */
        public static Func<StayCheck, string, IQueryable<string>> SearchID1
        {
            get
            {
                 Func<StayCheck, string, IQueryable<string>> searchID1 =
        CompiledQuery.Compile((StayCheck db, string id) =>
            from c in db.CheckIfOverTime where c.Uid == id select c.Uid);
                return searchID1;
            }
        }
        public static Func<StayCheck, string, IQueryable<string>> SearchID2
        {
            get
            {
                Func<StayCheck, string, IQueryable<string>> searchID2 =
        CompiledQuery.Compile((StayCheck db, string id) =>
            from c in db.CheckIfOverTime2 where c.Uid == id select c.Uid);
                return searchID2;
            }
        }

    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace PostgresqlTest
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                for (int i = 0; i < 10; i++)
                {
                    Thread sqlThread = new Thread(() => { sqlTest(); });
                    sqlThread.Start();
                }
            }
            catch (Exception ex)
            {

                Console.WriteLine(ex);
            }
            finally
            {
                Console.ReadLine();
            }
            
        }

        private static void sqlTest()
        {
            SqlClient sqlClient = new SqlClient(
                "127.0.0.1",
                "5432",
                "postgres",
                "postgres",
                "tms2",
                "true",
                "0",
                "20",
                "0");
            sqlClient.connect();
            sqlClient.modify(@"INSERT INTO test(
            sn, text)
    VALUES ((select count(sn) from test)::text || 'dd', 'cc');");
            sqlClient.disconnect();
            sqlClient.Dispose();
        }
    }
}

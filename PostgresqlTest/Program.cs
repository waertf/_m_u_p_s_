using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using Gurock.SmartInspect;

namespace PostgresqlTest
{
    class Program
    {
        static object lockSql = new object();
        private static int i;
        static SqlClient sqlClient = new SqlClient(
                "127.0.0.1",
                "5432",
                "postgres",
                "postgres",
                "testdb",
                "true",
                "0",
                "2",
                "0");
        static void Main(string[] args)
        {
            SiAuto.Si.Enabled = true;
            SiAuto.Si.Connections = @"file(filename=""" +
                                    Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) +
                                    "\\log.sil\",rotate=weekly,append=true,maxparts=5,maxsize=500MB)";
            string logMsg = string.Empty;
            sqlClient.connect();
            try
            {
                for (int i = 0; i < 10000; i++)
                {
                    Thread sqlThread = new Thread(() => { sqlTest(); });
                    sqlThread.Start();
                    //Thread.Sleep(50);
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

            i++;
            Console.WriteLine("+"+i);
            lock (lockSql)
            {
                sqlClient.get_DataTable(@"SELECT 
  public.emp.empno,
  public.emp.ename,
  public.emp.job,
  public.emp.mgr,
  public.emp.hiredate,
  public.emp.sal,
  public.emp.comm,
  public.emp.deptno
FROM
  public.emp");
            }
            Console.WriteLine("-" + i);
            //sqlClient.disconnect();
            //sqlClient.Dispose();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Devart.Data.PostgreSql;
using Gurock.SmartInspect;

namespace WhatsUpSqlClient
{
    class SqlClient
    {
        PgSqlConnectionStringBuilder pgCSB = null;
        public SqlClient(string ip, string port, string user_id, string password, string database)
        {
            pgCSB = new PgSqlConnectionStringBuilder();
            pgCSB.Host = ip;
            pgCSB.Port = int.Parse(port);
            pgCSB.UserId = user_id;
            pgCSB.Password = password;
            pgCSB.Database = database;
            pgCSB.Unicode = true;
            
            SiAuto.Si.Enabled = true;
            SiAuto.Si.Level = Level.Debug;
            SiAuto.Si.Connections = @"file(filename=""" +
                                    Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) +
                                    "\\sqllog.sil\",rotate=weekly,append=true,maxparts=5,maxsize=500MB)";
        }

        public void LoadDatatable(DataTable dt)
        {
            using (PgSqlConnection pgSqlConnection = new PgSqlConnection(pgCSB.ConnectionString))
            {
                using (PgSqlLoader loader = new PgSqlLoader())
                {
                    try
                    {
                        loader.Connection = pgSqlConnection;
                        loader.TableName = "custom.WhatsUpDeviceStatus";
                        pgSqlConnection.Open();
                        loader.Open();
                        //loader.CreateColumns();
                        loader.LoadTable(dt);
                    }
                    catch (Exception e)
                    {

                        Console.WriteLine("error:" + e.ToString());
                        SiAuto.Main.LogException(e);
                    }
                    finally
                    {
                        loader.Close();
                        pgSqlConnection.Close();
                    }
                }
            }
        }
        public void SqlScriptCmd(string script)
        {
            using (PgSqlConnection pgSqlConnection = new PgSqlConnection(pgCSB.ConnectionString))
            {
                try
                {
                    PgSqlScript pgscScript = new PgSqlScript(script,pgSqlConnection);
                    pgscScript.Progress += pgscScript_Progress;
                    pgscScript.Error += pgscScript_Error;
                    pgSqlConnection.Open();
                    pgscScript.Execute();
                }
                catch (Exception e)
                {
                    Console.WriteLine("error:"+e.ToString());
                    SiAuto.Main.LogException(e);
                }
                finally
                {
                    pgSqlConnection.Close();
                }
            }
        }

        void pgscScript_Error(object sender, Devart.Common.ScriptErrorEventArgs e)
        {
            e.Ignore = true;
            Console.WriteLine(e.Text);
            Console.WriteLine("  Failed."); 
            SiAuto.Main.LogError(e.Text);
        }

        void pgscScript_Progress(object sender, Devart.Common.ScriptProgressEventArgs e)
        {
            Console.WriteLine(e.Text);
            Console.WriteLine("  Successfully executed."); 
        }
    }
}

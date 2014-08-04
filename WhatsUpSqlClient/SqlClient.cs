using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
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
        public DataTable get_DataTable(string cmd)
        {
            PgSqlCommand command = null;
            using (PgSqlConnection pgSqlConnection = new PgSqlConnection(pgCSB.ConnectionString))
            using (DataTable datatable = new DataTable())
            {
                try
                {

                    {
                        //DataTable datatable = new DataTable();
                        command = pgSqlConnection.CreateCommand();
                        command.CommandText = cmd;
                        //command.CommandTimeout = 30;
                        //Console.WriteLine("Starting asynchronous retrieval of data...");
                        PgSqlDataReader myReader;

                        //IAsyncResult cres = command.BeginExecuteReader();
                        //Console.Write("In progress...");
                        //while (!cres.IsCompleted)
                        {
                            //Console.Write(".");
                            //Perform here any operation you need
                        }

                        //if (cres.IsCompleted)
                        //Console.WriteLine("Completed.");
                        //else
                        //Console.WriteLine("Have to wait for operation to complete...");
                        //PgSqlDataReader myReader = command.EndExecuteReader(cres);
                        //PgSqlDataReader myReader = command.ExecuteReader();
                        try
                        {
                            {


                                //IAsyncResult cres = command.BeginExecuteReader();
                                //myReader = command.EndExecuteReader(cres);
                                //lock (accessLock)
                                pgSqlConnection.Open();
                                myReader = command.ExecuteReader();
                                //stopWatch.Stop();
                                // Get the elapsed time as a TimeSpan value.


                                // Format and display the TimeSpan value.
                                
                                for (int i = 0; i < myReader.FieldCount; i++)
                                {
                                    //Console.Write(myReader.GetName(i).ToString() + "\t");
                                    datatable.Columns.Add(myReader.GetName(i).ToString(), typeof(string));
                                }
                                //stopWatch.Stop();
                                
                                while (myReader.Read())
                                {
                                    DataRow dr = datatable.NewRow();

                                    for (int i = 0; i < myReader.FieldCount; i++)
                                    {
                                        //Console.Write(myReader.GetString(i) + "\t");
                                        dr[i] = myReader.GetString(i);
                                    }
                                    datatable.Rows.Add(dr);
                                    //Console.Write(Environment.NewLine);
                                    //Console.WriteLine(myReader.GetInt32(0) + "\t" + myReader.GetString(1) + "\t");
                                }
                                myReader.Close();
                                
                                //myReader.Dispose();
                            }
                        }
                        finally
                        {


                        }
                        /*
                        foreach (DataRow row in datatable.Rows) // Loop over the rows.
                        {
                            Console.WriteLine("--- Row ---"); // Print separator.
                            foreach (var item in row.ItemArray) // Loop over the items.
                            {
                                Console.Write("Item: "); // Print label.
                                Console.WriteLine(item); // Invokes ToString abstract method.
                            }
                        }
                        */
                        
                        
                        //if (command != null)
                        //command.Dispose();
                        command = null;
                        using (DataTable returnTable = datatable.Copy())
                        {
                            
                            return returnTable;
                        }
                        //DataTable returnTable = datatable.Copy();

                    }
                    

                }
                catch (PgSqlException ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("GetDataTable exception occurs: {0}" + Environment.NewLine + "{1}", ex.Error, cmd);
                    
                    Console.ResetColor();
                    //if (command != null)
                    //command.Dispose();
                    command = null;
                    return null;
                }
            }

        }
        public void modify(string cmd)
        {

            PgSqlCommand command = null;
            PgSqlTransaction myTrans = null;
            using (PgSqlConnection pgSqlConnection = new PgSqlConnection(pgCSB.ConnectionString))
            try
            {
                
                {
                    //insert
                    command = pgSqlConnection.CreateCommand();
                    command.CommandText = cmd;
                    //command.CommandTimeout = 30;

                    //cmd.CommandText = "INSERT INTO public.test (id) VALUES (1)";
                    //pgSqlConnection.BeginTransaction();
                    //async
                    int RowsAffected;



                    {
                        pgSqlConnection.Open();
                        myTrans = pgSqlConnection.BeginTransaction(IsolationLevel.ReadCommitted);
                        command.Transaction = myTrans;
                        //IAsyncResult cres = command.BeginExecuteNonQuery();
                        //RowsAffected = command.EndExecuteNonQuery(cres);
                        //lock (accessLock)
                        RowsAffected = command.ExecuteNonQuery();
                        myTrans.Commit();
                    }
                    //IAsyncResult cres=command.BeginExecuteNonQuery(null,null);
                    //Console.Write("In progress...");
                    //while (!cres.IsCompleted)
                    {
                        //Console.Write(".");
                        //Perform here any operation you need
                    }
                    /*
                    if (cres.IsCompleted)
                        Console.WriteLine("Completed.");
                    else
                        Console.WriteLine("Have to wait for operation to complete...");
                    */
                    //int RowsAffected = command.EndExecuteNonQuery(cres);
                    //Console.WriteLine("Done. Rows affected: " + RowsAffected.ToString());

                    //sync
                    //int aff = command.ExecuteNonQuery();
                    //Console.WriteLine(RowsAffected + " rows were affected.");
                    //command.Dispose();
                    command = null;
                    //pgSqlConnection.Commit();
                    /*
                    ThreadPool.QueueUserWorkItem(callback =>
                    {
                        
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.WriteLine(RowsAffected + " rows were affected.");
                        Console.WriteLine(
                            "S++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++");
                        Console.WriteLine("sql Write:\r\n" + cmd);
                        Console.WriteLine(
                            "E++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++");
                        Console.ResetColor();
                        log.Info("sql Write:\r\n" + cmd);
                    });
                    */


                    // Format and display the TimeSpan value.
                    

                }

            }
            catch (PgSqlException ex)
            {
                if (myTrans != null) myTrans.Rollback();
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Modify exception occurs: {0}" + Environment.NewLine + "{1}", ex.Error, cmd);
                
                Console.ResetColor();
                //pgSqlConnection.Rollback();
                //command.Dispose();
                command = null;


            }


        }
    }
}

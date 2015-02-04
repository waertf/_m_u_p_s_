using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using Devart.Data.PostgreSql;
using System.Data;
using Gurock.SmartInspect;
using System.Configuration;
using log4net;
using log4net.Config;

namespace ConsoleApplication1_client_threading
{
    class SqlClient
    {

        //PgSqlConnection pgSqlConnection; 
        PgSqlConnectionStringBuilder pgCSB2 = new PgSqlConnectionStringBuilder();
        PgSqlConnectionStringBuilder pgCSB = new PgSqlConnectionStringBuilder();
        public bool IsConnected { get; set; }
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        readonly object accessLock = new object();
        public SqlClient(string ip, string port, string user_id, string password, string database, string Pooling, string MinPoolSize, string MaxPoolSize, string ConnectionLifetime)
        {
            //PgSqlConnectionStringBuilder pgCSB = new PgSqlConnectionStringBuilder();
            pgCSB.Host = ip;
            pgCSB.Port =pgCSB2.Port= int.Parse(port);
            pgCSB.UserId = pgCSB2.UserId=user_id;
            pgCSB.Password = pgCSB2.Password=password;
            pgCSB.Database = pgCSB2.Database=database;

            //pgCSB.Pooling = bool.Parse(Pooling);
            //pgCSB.MinPoolSize = int.Parse(MinPoolSize);
            pgCSB.MaxPoolSize = pgCSB2.MaxPoolSize = 20;
            //pgCSB.ConnectionLifetime = int.Parse(ConnectionLifetime); ;
            //pgCSB.ConnectionTimeout = 15;
            pgCSB.Unicode = true;
            //pgSqlConnection = new PgSqlConnection(pgCSB.ConnectionString);
            //IsConnected = false;

            
            pgCSB2.Host = ConfigurationManager.AppSettings["DB2_ADDRESS"];
            pgCSB2.Unicode = true;
        }
        public void connect()
        {
            return;
            try
            {
                //pgSqlConnection.Open();
                IsConnected = true;
                /*
                {
                    if (pgSqlConnection != null)
                    {
                        pgSqlConnection.Open();
                        IsConnected = true;
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                */
                
            }
            catch (PgSqlException ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Connect exception occurs: {0}", ex.Error);
                log.Error("Connect exception occurs: " + ex.Error);
                Console.ResetColor();
                disconnect();
                connect();
            }
        }
        public bool disconnect()
        {
            return true;
            try
            {
                //if (pgSqlConnection != null)
                {
                    //pgSqlConnection.Close();
                    IsConnected = false;
                    return true;
                }
                //else
                {
                    return false;
                }
            }
            catch (PgSqlException ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Disconnect exception occurs: {0}", ex.Error);
                log.Error("Disconnect exception occurs: " + ex.Error);
                Console.ResetColor();
                return false;
            }
        }
        //For UPDATE, INSERT, and DELETE statements
        public void modify(string cmd)
        {
            if(string.IsNullOrEmpty(cmd))
                return;;
            /*
            System.Threading.Thread accessDb2Thread = new System.Threading.Thread
      (delegate()
      {
          modifyDB2(cmd);
      });
            accessDb2Thread.Start();
            */
            //modifyDB2(cmd);
            Stopwatch stopWatch = new Stopwatch();
            //PgSqlCommand command = null;
            PgSqlTransaction myTrans = null;
            using (var pgSqlConnection = new PgSqlConnection(pgCSB.ConnectionString))
            using (PgSqlCommand command = new PgSqlCommand())
            try
            {
                //if (pgSqlConnection != null && IsConnected)
                
                //{
                    //insert
                    //pgSqlConnection.Open();
                    //PgSqlCommand command = new PgSqlCommand();
                    command.Connection = pgSqlConnection;
                    command.UnpreparedExecute = true;
                    command.CommandType=CommandType.Text;
                    command.CommandText = string.Copy(cmd);
                    //command.CommandTimeout = 30;

                    //cmd.CommandText = "INSERT INTO public.test (id) VALUES (1)";
                    //pgSqlConnection.BeginTransaction();
                    //async
                    int RowsAffected;
                    
                    
                    //lock (accessLock)
                    //{
                        pgSqlConnection.Open();
                        //myTrans = pgSqlConnection.BeginTransaction(IsolationLevel.ReadCommitted);
                        //command.Transaction = myTrans;
                        //IAsyncResult cres = command.BeginExecuteNonQuery();
                        //RowsAffected = command.EndExecuteNonQuery(cres);
                        //lock (accessLock)
                        RowsAffected = command.ExecuteNonQuery();
                        //myTrans.Commit();
                        //pgSqlConnection.Close();
                    //}
                
                    //IAsyncResult cres=command.BeginExecuteNonQuery(null,null);
                    //Console.Write("In progress...");
                    //while (!cres.IsCompleted)
                    //{
                        //Console.Write(".");
                        //Perform here any operation you need
                    //}
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
                    //command = null;
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
                    stopWatch.Stop();
                    // Get the elapsed time as a TimeSpan value.
                    TimeSpan ts = stopWatch.Elapsed;

                    // Format and display the TimeSpan value.
                    string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
                        ts.Hours, ts.Minutes, ts.Seconds,
                        ts.Milliseconds / 10);
                    SiAuto.Main.AddCheckpoint(Level.Debug, "sql modify take time:" + elapsedTime, cmd);
                    
                //}

            }
            catch (PgSqlException ex)
            {
                if (myTrans != null) myTrans.Rollback();
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Modify exception occurs: {0}" + Environment.NewLine + "{1}", ex.Error, cmd);
                log.Error("Modify exception occurs: " + Environment.NewLine + ex.Error + Environment.NewLine + cmd);
                Console.ResetColor();
                //pgSqlConnection.Rollback();
                //command.Dispose();
                //command = null;

            }

            //accessDb2Thread.Join();
        }
        void EndModify(IAsyncResult ar)
        {
            PgSqlCommand command = null;
            try
            {
                command = ar.AsyncState as PgSqlCommand;
                int row = command.EndExecuteNonQuery(ar);
                //Console.WriteLine(row + " rows were affected.");
            }
            catch (PgSqlException ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Modify exception occurs: {0}" + Environment.NewLine + "{1}", ex.Error, command.CommandText);
                log.Error("Modify exception occurs: " + Environment.NewLine + ex.Error + Environment.NewLine + command.CommandText);
                Console.ResetColor();
                //pgSqlConnection.Rollback();
            }
            finally
            {
                if (command != null)
                    command.Dispose();
                command = null;
            }
        }
        //For SELECT statements
        public DataTable get_DataTable(string cmd,int timeout=30)
        {
            
            using (DataTable datatable = new DataTable())
            using (var pgSqlConnection = new PgSqlConnection(pgCSB.ConnectionString))
            using (PgSqlCommand command = new PgSqlCommand())
            {
                
                try
                {
                    //if (pgSqlConnection != null && IsConnected)
                    //{
                        //pgSqlConnection.Open();
                        //DataTable datatable = new DataTable();
                        command.Connection = pgSqlConnection;
                    command.UnpreparedExecute = true;
                        command.CommandText = cmd;
                    command.CommandTimeout = timeout;
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
                        //try
                        //{
                            //lock (accessLock)
                            {

                                Stopwatch stopWatch = new Stopwatch();
                                stopWatch.Start();
                                //IAsyncResult cres = command.BeginExecuteReader();
                                //myReader = command.EndExecuteReader(cres);
                                //lock (accessLock)
                                pgSqlConnection.Open();
                                myReader = command.ExecuteReader();
                                //stopWatch.Stop();
                                // Get the elapsed time as a TimeSpan value.
                                TimeSpan ts = stopWatch.Elapsed;

                                // Format and display the TimeSpan value.
                                string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
                                    ts.Hours, ts.Minutes, ts.Seconds,
                                    ts.Milliseconds / 10);
                                SiAuto.Main.AddCheckpoint(Level.Debug, "sql query take time:" + elapsedTime, cmd);
                                // printing the column names
                                stopWatch.Reset();
                                //stopWatch.Start();
                                for (int i = 0; i < myReader.FieldCount; i++)
                                {
                                    //Console.Write(myReader.GetName(i).ToString() + "\t");
                                    datatable.Columns.Add(myReader.GetName(i).ToString(), typeof (string));
                                }
                                //stopWatch.Stop();
                                //ts = stopWatch.Elapsed;
                                //elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
                                    //ts.Hours, ts.Minutes, ts.Seconds,
                                    //ts.Milliseconds / 10);
                                //SiAuto.Main.AddCheckpoint(Level.Debug, "sql query2 take time:" + elapsedTime, cmd);
                                //Console.Write(Environment.NewLine);
                                //stopWatch.Reset();
                                //stopWatch.Start();
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
                                //pgSqlConnection.Close();
                                //stopWatch.Stop();
                                //ts = stopWatch.Elapsed;
                               // elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
                                    //ts.Hours, ts.Minutes, ts.Seconds,
                                    //ts.Milliseconds / 10);
                                //SiAuto.Main.AddCheckpoint(Level.Debug, "sql query3 take time:" + elapsedTime, cmd);
                                //myReader.Dispose();
                            }
                        //}
                        //finally
                        //{
                            
                            
                        //}
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
                        //Stopwatch stopWatch2= new Stopwatch();
                        //stopWatch2.Start();
                        //if (command != null)
                            //command.Dispose();
                        //command = null;
                        using (DataTable returnTable = datatable.Copy())
                        {
                            //stopWatch2.Stop();
                            //SiAuto.Main.AddCheckpoint(Level.Debug, "sql query4 take time(ms):" + stopWatch2.ElapsedMilliseconds, cmd);
                            return returnTable;
                        }
                        //DataTable returnTable = datatable.Copy();
                        
                    //}
                    //else
                    //{

                        //return null;
                    //}

                }
                catch (PgSqlException ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("GetDataTable exception occurs: {0}" + Environment.NewLine + "{1}", ex.Error, cmd);
                    log.Error("GetDataTable exception occurs: " + Environment.NewLine + ex.Error + Environment.NewLine + cmd);
                    Console.ResetColor();
                    //if (command != null)
                        //command.Dispose();
                    //command = null;
                    return null;
                }
            }
            
        }
        public void Dispose()
        {
            return;
            //PgSqlConnection.ClearAllPools(true);
            //PgSqlConnection.ClearPool(pgSqlConnection);
            //pgSqlConnection.Dispose();
            //pgSqlConnection = null;
        }
        //~SqlClient()
        //{
        //PgSqlConnection.ClearPool(pgSqlConnection);
        //pgSqlConnection.Dispose();
        //pgSqlConnection = null;
        //} 
        void modifyDB2(string cmd)
        {
            if (string.IsNullOrEmpty(cmd))
                return; ;
            //Stopwatch stopWatch = new Stopwatch();
            //PgSqlCommand command = null;
            PgSqlTransaction myTrans = null;
            using (var pgSqlConnection2 = new PgSqlConnection(pgCSB2.ConnectionString))
            using (PgSqlCommand command = new PgSqlCommand())
            try
            {
                
                //{
                    //pgSqlConnection2.Open();
                    //insert
                    //PgSqlCommand command = new PgSqlCommand();
                    command.Connection = pgSqlConnection2;
                    command.UnpreparedExecute = true;
                    command.CommandType = CommandType.Text;
                    command.CommandText = string.Copy(cmd);
                    //command.CommandTimeout = 30;

                    //cmd.CommandText = "INSERT INTO public.test (id) VALUES (1)";
                    //pgSqlConnection.BeginTransaction();
                    //async
                    int RowsAffected;


                    //lock (accessLock)
                    //{
                    pgSqlConnection2.Open();
                        //myTrans = pgSqlConnection2.BeginTransaction(IsolationLevel.ReadCommitted);
                        //command.Transaction = myTrans;
                        //IAsyncResult cres = command.BeginExecuteNonQuery();
                        //RowsAffected = command.EndExecuteNonQuery(cres);
                        //lock (accessLock)
                        RowsAffected = command.ExecuteNonQuery();
                       // myTrans.Commit();
                    //}
                    //pgSqlConnection2.Close();
                    //IAsyncResult cres=command.BeginExecuteNonQuery(null,null);
                    //Console.Write("In progress...");
                    //while (!cres.IsCompleted)
                    //{
                        //Console.Write(".");
                        //Perform here any operation you need
                    //}
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
                    //command = null;
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
                    //stopWatch.Stop();
                    // Get the elapsed time as a TimeSpan value.
                    //TimeSpan ts = stopWatch.Elapsed;

                    // Format and display the TimeSpan value.
                    //string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
                        //ts.Hours, ts.Minutes, ts.Seconds,
                        //ts.Milliseconds / 10);
                    //SiAuto.Main.AddCheckpoint(Level.Debug, "sql modify take time:" + elapsedTime, cmd);

                //}

            }
            catch (PgSqlException ex)
            {
                if (myTrans != null) myTrans.Rollback();
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Modify exception occurs: {0}" + Environment.NewLine + "{1}", ex.Error, cmd);
                log.Error("Modify exception occurs: " + Environment.NewLine + ex.Error + Environment.NewLine + cmd);
                Console.ResetColor();
                //pgSqlConnection.Rollback();
                //command.Dispose();
                //command = null;
                //pgSqlConnection2.Close();

            }


        }
    }
}

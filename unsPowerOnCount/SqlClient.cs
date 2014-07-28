using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using Devart.Data.PostgreSql;
using System.Data;


namespace unsPowerOnCount
{
    class SqlClient
    {

        PgSqlConnection pgSqlConnection;
        public bool IsConnected { get; set; }
        
        object accessLock = new object();
        public SqlClient(string ip, string port, string user_id, string password, string database, string Pooling, string MinPoolSize, string MaxPoolSize, string ConnectionLifetime)
        {
            PgSqlConnectionStringBuilder pgCSB = new PgSqlConnectionStringBuilder();
            pgCSB.Host = ip;
            pgCSB.Port = int.Parse(port);
            pgCSB.UserId = user_id;
            pgCSB.Password = password;
            pgCSB.Database = database;

            pgCSB.Pooling = bool.Parse(Pooling);
            pgCSB.MinPoolSize = int.Parse(MinPoolSize);
            pgCSB.MaxPoolSize = int.Parse(MaxPoolSize);
            pgCSB.ConnectionLifetime = int.Parse(ConnectionLifetime); ;
            pgCSB.ConnectionTimeout = 15;
            pgCSB.Unicode = true;
            pgSqlConnection = new PgSqlConnection(pgCSB.ConnectionString);
            IsConnected = false;
        }
        public void connect()
        {
            try
            {
                pgSqlConnection.Open();
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

                Console.ResetColor();
                disconnect();
                connect();
            }
        }
        public bool disconnect()
        {
            try
            {
                if (pgSqlConnection != null)
                {
                    pgSqlConnection.Close();
                    IsConnected = false;
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (PgSqlException ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Disconnect exception occurs: {0}", ex.Error);

                Console.ResetColor();
                return false;
            }
        }
        //For UPDATE, INSERT, and DELETE statements
        public void modify(string cmd)
        {
            Stopwatch stopWatch = new Stopwatch();
            PgSqlCommand command = null;
            PgSqlTransaction myTrans = null;
            try
            {
                if (pgSqlConnection != null && IsConnected)
                {
                    //insert
                    command = pgSqlConnection.CreateCommand();
                    command.CommandText = cmd;
                    //command.CommandTimeout = 30;

                    //cmd.CommandText = "INSERT INTO public.test (id) VALUES (1)";
                    //pgSqlConnection.BeginTransaction();
                    //async
                    int RowsAffected;
                    
                    myTrans = pgSqlConnection.BeginTransaction(IsolationLevel.ReadCommitted);
                    command.Transaction = myTrans;
                    //lock (accessLock)
                    {
                        //IAsyncResult cres = command.BeginExecuteNonQuery();
                        //RowsAffected = command.EndExecuteNonQuery(cres);
                        lock (accessLock)
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
                    stopWatch.Stop();
                    // Get the elapsed time as a TimeSpan value.
                    TimeSpan ts = stopWatch.Elapsed;

                    // Format and display the TimeSpan value.
                    string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
                        ts.Hours, ts.Minutes, ts.Seconds,
                        ts.Milliseconds / 10);
                    

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
               
                Console.ResetColor();
                pgSqlConnection.Rollback();
            }
            finally
            {
                if (command != null)
                    command.Dispose();
                command = null;
            }
        }
        //For SELECT statements
        public DataTable get_DataTable(string cmd)
        {
            PgSqlCommand command = null;
            
            using (DataTable datatable = new DataTable())
            {
                try
                {
                    if (pgSqlConnection != null && IsConnected)
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
                            lock (accessLock)
                            {

                                Stopwatch stopWatch = new Stopwatch();
                                stopWatch.Start();
                                //IAsyncResult cres = command.BeginExecuteReader();
                                //myReader = command.EndExecuteReader(cres);
                                //lock (accessLock)
                                myReader = command.ExecuteReader();
                                //stopWatch.Stop();
                                // Get the elapsed time as a TimeSpan value.
                                TimeSpan ts = stopWatch.Elapsed;

                                // Format and display the TimeSpan value.
                                string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
                                    ts.Hours, ts.Minutes, ts.Seconds,
                                    ts.Milliseconds / 10);
                                
                                // printing the column names
                                stopWatch.Reset();
                                stopWatch.Start();
                                for (int i = 0; i < myReader.FieldCount; i++)
                                {
                                    //Console.Write(myReader.GetName(i).ToString() + "\t");
                                    datatable.Columns.Add(myReader.GetName(i).ToString(), typeof (string));
                                }
                                //stopWatch.Stop();
                                ts = stopWatch.Elapsed;
                                elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
                                    ts.Hours, ts.Minutes, ts.Seconds,
                                    ts.Milliseconds / 10);
                                
                                //Console.Write(Environment.NewLine);
                                stopWatch.Reset();
                                stopWatch.Start();
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
                                stopWatch.Stop();
                                ts = stopWatch.Elapsed;
                                elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
                                    ts.Hours, ts.Minutes, ts.Seconds,
                                    ts.Milliseconds / 10);
                                
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
                        Stopwatch stopWatch2= new Stopwatch();
                        stopWatch2.Start();
                        //if (command != null)
                            //command.Dispose();
                        command = null;
                        using (DataTable returnTable = datatable.Copy())
                        {
                            stopWatch2.Stop();
                            
                            return returnTable;
                        }
                        //DataTable returnTable = datatable.Copy();
                        
                    }
                    else
                    {

                        return null;
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
        public void Dispose()
        {
            //PgSqlConnection.ClearAllPools(true);
            PgSqlConnection.ClearPool(pgSqlConnection);
            //pgSqlConnection.Dispose();
            pgSqlConnection = null;
        }
        //~SqlClient()
        //{
        //PgSqlConnection.ClearPool(pgSqlConnection);
        //pgSqlConnection.Dispose();
        //pgSqlConnection = null;
        //}   
    }
}

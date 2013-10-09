﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Devart.Data.PostgreSql;
using System.Data;

namespace server_syn_console
{
    class SqlClient
    {
        PgSqlConnectionStringBuilder pgCSB = new PgSqlConnectionStringBuilder();
        PgSqlConnection pgSqlConnection;
        public bool IsConnected { get; set; }
        public SqlClient(string ip, string port, string user_id, string password, string database)
        {
            pgCSB.Host = ip;
            pgCSB.Port = int.Parse(port);
            pgCSB.UserId = user_id;
            pgCSB.Password = password;
            pgCSB.Database = database;
            pgCSB.MaxPoolSize = 150;
            pgCSB.ConnectionTimeout = 30;
            pgCSB.Unicode = true;
            pgSqlConnection = new PgSqlConnection(pgCSB.ConnectionString);
        }
        public bool connect()
        {
            try
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
            catch (PgSqlException ex)
            {
                Console.WriteLine("connect Exception occurs: {0}", ex.Error);
                return false;
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
                Console.WriteLine("disconnect Exception occurs: {0}", ex.Error);
                return false;
            }
        }
        //For UPDATE, INSERT, and DELETE statements
        public bool modify(string cmd)
        {
            try
            {
                if (pgSqlConnection != null && IsConnected)
                {
                    //insert
                    PgSqlCommand command = pgSqlConnection.CreateCommand();
                    command.CommandText = cmd;
                    //cmd.CommandText = "INSERT INTO public.test (id) VALUES (1)";

                    //async
                    IAsyncResult cres = command.BeginExecuteNonQuery(null, null);

                    if (cres.IsCompleted)
                        Console.WriteLine("Completed.");
                    else
                        Console.WriteLine("Have to wait for operation to complete...");
                    int RowsAffected = command.EndExecuteNonQuery(cres);
                    Console.WriteLine("Done. Rows affected: " + RowsAffected.ToString());
                    /*
                     //sync
                     int aff = cmd.ExecuteNonQuery();
                     Console.WriteLine(aff + " rows were affected.");
                     * 
                     */
                    return true;
                }
                else
                    return false;
            }
            catch (PgSqlException ex)
            {
                Console.WriteLine("modify Exception occurs: {0}", ex.Error);
                return false;
            }

        }
        //For SELECT statements
        public DataTable get_DataTable(string cmd)
        {
            try
            {
                if (pgSqlConnection != null && IsConnected)
                {
                    DataTable datatable = new DataTable();
                    PgSqlCommand command = pgSqlConnection.CreateCommand();
                    command.CommandText = cmd;
                    Console.WriteLine("Starting asynchronous retrieval of data...");
                    IAsyncResult cres = command.BeginExecuteReader();
                    if (cres.IsCompleted)
                        Console.WriteLine("Completed.");
                    else
                        Console.WriteLine("Have to wait for operation to complete...");
                    PgSqlDataReader myReader = command.EndExecuteReader(cres);
                    try
                    {
                        // printing the column names
                        for (int i = 0; i < myReader.FieldCount; i++)
                        {
                            Console.Write(myReader.GetName(i).ToString() + "\t");
                            datatable.Columns.Add(myReader.GetName(i).ToString(), typeof(string));
                        }
                        Console.Write(Environment.NewLine);
                        while (myReader.Read())
                        {
                            DataRow dr = datatable.NewRow();

                            for (int i = 0; i < myReader.FieldCount; i++)
                            {
                                Console.Write(myReader.GetString(i) + "\t");
                                dr[i] = myReader.GetString(i);
                            }
                            datatable.Rows.Add(dr);
                            Console.Write(Environment.NewLine);
                            Console.WriteLine(myReader.GetInt32(0) + "\t" + myReader.GetString(1) + "\t");
                        }
                    }
                    finally
                    {
                        myReader.Close();
                    }
                    foreach (DataRow row in datatable.Rows) // Loop over the rows.
                    {
                        Console.WriteLine("--- Row ---"); // Print separator.
                        foreach (var item in row.ItemArray) // Loop over the items.
                        {
                            Console.Write("Item: "); // Print label.
                            Console.WriteLine(item); // Invokes ToString abstract method.
                        }
                    }
                    return datatable;
                }
                else
                    return null;
            }
            catch (PgSqlException ex)
            {
                Console.WriteLine("get_DataTable Exception occurs: {0}", ex.Error);
                return null;
            }
        }

    }
}

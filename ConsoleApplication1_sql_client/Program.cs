using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Devart.Data.PostgreSql;
using System.Data;



namespace ConsoleApplication1_sql_client
{
    class Program
    {
        static void Main(string[] args)
        {
            
            PgSqlConnectionStringBuilder pgCSB = new PgSqlConnectionStringBuilder();
            pgCSB.Host = "192.168.1.78";
            pgCSB.Port = 5432;
            pgCSB.UserId = "postgres";
            pgCSB.Password = "postgres";
            pgCSB.Database = "tms2";
            pgCSB.MaxPoolSize = 150;
            pgCSB.ConnectionTimeout = 30;
            pgCSB.Unicode = true;
            PgSqlConnection pgSqlConnection = new PgSqlConnection(pgCSB.ConnectionString);
            try
            {
                pgSqlConnection.Open();

                Count(pgSqlConnection);


            }
            catch (PgSqlException ex)
            {
                Console.WriteLine("Exception occurs: {0}", ex.Error);

            }
            finally
            {
                pgSqlConnection.Close();
                Console.ReadLine();
            }

        }
        static void Insert(PgSqlConnection connection)
        {
            //insert
            PgSqlCommand command = connection.CreateCommand();
            command.CommandText = "INSERT INTO public._gps_log (_id,_uid,_status,_time,_validity,_lat,_lon,_speed,_course,_distance,_judgement,_or_lon,_or_lat,_satellites,_temperature,_voltage) VALUES (1681185,'_uid',2,'2012-10-01 18:32:50.553+08','a',25.062923,121.522705,0,0,0,0,0,0,3,35,100)";
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
            */

        }
        static void PrintDept(PgSqlConnection connection)
        {
            PgSqlCommand command = connection.CreateCommand();
            command.CommandText = "select * from test";
            //async
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
                    Console.Write(myReader.GetName(i).ToString() + "\t");
                Console.Write(Environment.NewLine);
                while (myReader.Read())
                {
                    for (int i = 0; i < myReader.FieldCount; i++)
                        Console.Write(myReader.GetString(i) + "\t");
                    Console.Write(Environment.NewLine);
                    //Console.WriteLine(myReader.GetInt32(0) + "\t" + myReader.GetString(1) + "\t");
                }
            }
            finally
            {
                myReader.Close();
            }
            /*
            // Call the Close method when you are finished using the PgSqlDataReader 
            // to use the associated PgSqlConnection for any other purpose.
            // Or put the reader in the using block to call Close implicitly.
            //sync
            Console.WriteLine("Starting synchronous retrieval of data...");
            using (PgSqlDataReader reader = command.ExecuteReader())
            {
                // printing the column names
                for (int i = 0; i < reader.FieldCount; i++)
                    Console.Write(reader.GetName(i).ToString() + "\t");
                Console.Write(Environment.NewLine);
                // Always call Read before accesing data
                while (reader.Read())
                {
                    // printing the table content
                    for (int i = 0; i < reader.FieldCount; i++)
                        Console.Write(reader.GetValue(i).ToString() + "\t");
                    Console.Write(Environment.NewLine);
                }
            }
             * */
        }

        static void ModifyDept(PgSqlConnection connection)
        {
            PgSqlCommand command = connection.CreateCommand();
            command.CommandText = "UPDATE public.test SET test='test' WHERE id > 1";

            // return value of ExecuteNonQuery (i) is the number of rows affected by the command
            int i = command.ExecuteNonQuery();
            Console.WriteLine(Environment.NewLine + "Rows in DEPT updated: {0}", i + Environment.NewLine);
        }

        static void Count(PgSqlConnection connection)
        {
            DataTable datatable = new DataTable();
            PgSqlCommand command = connection.CreateCommand();
            command.CommandText = @"SELECT COUNT
                                    (_uid)   
                                    FROM public._gps_log";
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
                    //Console.WriteLine(myReader.GetInt32(0) + "\t" + myReader.GetString(1) + "\t");
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
            Console.WriteLine("############");
            Console.WriteLine(datatable.Rows[0].ItemArray[0].ToString());

        }

    }
}

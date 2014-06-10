using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using Npgsql;

namespace ConsoleApplication1_client_threading
{
    class NpgSqlClient
    {
        private NpgsqlConnection conn=null;
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        
        public NpgSqlClient(string ip, string port, string user_id, string password, string database, string Pooling,
            string MinPoolSize, string MaxPoolSize, string ConnectionLifetime)
        {
            NpgsqlConnectionStringBuilder connectionString = new NpgsqlConnectionStringBuilder();
            connectionString.Host = ip;
            connectionString.Port = int.Parse(port);
            connectionString.UserName = user_id;
            connectionString.Password = password;
            conn = new NpgsqlConnection(connectionString);
        }

        public bool connect()
        {
            try
            {
                conn.Open();
                return true;
            }
            catch (NpgsqlException e)
            {
                
                log.Error(e.Errors);
                return false;
            }
        }

        public bool disconnect()
        {
            try
            {
                conn.Close();
                return true;
            }
            catch (NpgsqlException e)
            {

                log.Error(e.Errors);
                return false;
            }
        }

        public void Dispose()
        {
            
        }
        public void modify(string cmd)
        {
            try
            {
                using (NpgsqlCommand command = new NpgsqlCommand(cmd, conn))
                {
                    command.ExecuteNonQuery();
                }    
            }
            catch (NpgsqlException e)
            {

                log.Error(e.Errors);
            }
        }

        public DataTable get_DataTable(string cmd)
        {
            try
            {
                using (NpgsqlDataAdapter adapter = new NpgsqlDataAdapter(cmd, conn))
                {
                    DataTable table = new DataTable();
                    adapter.Fill(table);
                    return table;
                }
            }
            catch (NpgsqlException e)
            {

                log.Error(e.Errors);
                return null;
            }
        }
    }
}

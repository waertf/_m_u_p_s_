using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Configuration;
using System.Windows.Forms;

namespace WindowsFormsApplication1__gps_log_export
{
    public partial class Form1 : Form
    {
        SqlClient sql_client = new SqlClient(ConfigurationManager.AppSettings["SQL_SERVER_IP"], ConfigurationManager.AppSettings["SQL_SERVER_PORT"], ConfigurationManager.AppSettings["SQL_SERVER_USER_ID"], ConfigurationManager.AppSettings["SQL_SERVER_PASSWORD"], ConfigurationManager.AppSettings["SQL_SERVER_DATABASE"]);
        public Form1()
        {
            InitializeComponent();
            /*
             * SELECT count(temp) FROM
(
select DISTINCT public._gps_log._uid from public._gps_log
) 
AS temp
             */
        }

        private void button1_Click(object sender, EventArgs e)
        {
            MessageBox.Show(FromDateTime.Value.ToString("yyyy-MM-dd HH:mm:ff"));
            resulttextBox.AppendText("test1"+Environment.NewLine);
            resulttextBox.AppendText("test2" + Environment.NewLine);
        }
    }
}

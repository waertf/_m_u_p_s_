using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace unsPowerOnCount
{
    public partial class Form1 : Form
    {
        SqlClient client = new SqlClient(
                ConfigurationManager.AppSettings["SQL_SERVER_IP"],
                ConfigurationManager.AppSettings["SQL_SERVER_PORT"],
                ConfigurationManager.AppSettings["SQL_SERVER_USER_ID"],
                ConfigurationManager.AppSettings["SQL_SERVER_PASSWORD"],
                ConfigurationManager.AppSettings["SQL_SERVER_DATABASE"],
                ConfigurationManager.AppSettings["Pooling"],
                ConfigurationManager.AppSettings["subMinPoolSize"],
                ConfigurationManager.AppSettings["subMaxPoolSize"],
                ConfigurationManager.AppSettings["subConnectionLifetime"]);
        public Form1()
        {
            InitializeComponent();
            System.Threading.Thread t1 = new System.Threading.Thread
      (delegate()
      {
          client.connect();
          string cmd = @"SELECT
Count(custom.uns_deivce_power_status.sn)
FROM
custom.uns_deivce_power_status
WHERE
custom.uns_deivce_power_status.""power"" = 'on'";
          while (true)
          {
              using (DataTable dt = client.get_DataTable(cmd))
              {
                  if (dt != null && dt.Rows.Count != 0)
                  {
                      string count = (dt.Rows[0].ItemArray[0]
                          .ToString());
                      MethodInvoker inv = delegate
                      {
                          this.label1.Text = count;
                      };

                    this.Invoke(inv);
                  }
                  else
                  {


                  } 
              }
              Thread.Sleep(1);
          }
      });
            t1.Start();
        }
    }
}

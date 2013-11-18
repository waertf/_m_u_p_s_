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
        SqlClient _sqlClient = new SqlClient(ConfigurationManager.AppSettings["SQL_SERVER_IP"], ConfigurationManager.AppSettings["SQL_SERVER_PORT"], ConfigurationManager.AppSettings["SQL_SERVER_USER_ID"], ConfigurationManager.AppSettings["SQL_SERVER_PASSWORD"], ConfigurationManager.AppSettings["SQL_SERVER_DATABASE"]);
        BackgroundWorker backgroundWorker1 = new BackgroundWorker();
        public Form1()
        {
            InitializeComponent();
            for (int i = 0; i < 60; i++)
            {
                min_comboBox.Items.Add(i);
            }
            min_comboBox.SelectedIndex = 0;
            /*
             * SELECT count(temp) FROM
(
select DISTINCT public._gps_log._uid from public._gps_log
where
public._gps_log._time > startTime
AND
public._gps_log._time < endTime
) 
AS temp
             */
        }

        private void SearchBtn_Click(object sender, EventArgs e)
        {
            //MessageBox.Show(FromDateTime.Value.ToString("yyyy-MM-dd HH:mm"));
            //resulttextBox.AppendText("test1"+Environment.NewLine);
            //resulttextBox.AppendText("test2" + Environment.NewLine);
            resulttextBox.Clear();
            if (_sqlClient.connect())
            {
                try
                {
                    List<string> listResult = new List<string>();
                    string numberResultCmd = @"SELECT count(temp) FROM
(
select DISTINCT public._gps_log._uid from public._gps_log
where
public._gps_log._time > '"+FromDateTime.Value.ToString("yyyy-MM-dd HH:mm")+@"'
AND
public._gps_log._time < '"+ToDateTime.Value.ToString("yyyy-MM-dd HH:mm")+@"'
) 
AS temp";
                    string listResultCmd = @"SELECT (temp) FROM
(
select DISTINCT public._gps_log._uid from public._gps_log
where
public._gps_log._time > '" + FromDateTime.Value.ToString("yyyy-MM-dd HH:mm") + @"'
AND
public._gps_log._time < '" + ToDateTime.Value.ToString("yyyy-MM-dd HH:mm") + @"'
) 
AS temp";
                    DataTable numberResult = _sqlClient.get_DataTable(numberResultCmd);
                    DataTable listResultDataTable = _sqlClient.get_DataTable(listResultCmd);
                    foreach (DataRow row in numberResult.Rows)
                    {
                        numberResultCmd = row[0].ToString();
                    }
                    foreach (DataRow VARIABLE in listResultDataTable.Rows)
                    {
                        listResult.Add(VARIABLE[0].ToString());
                    }
                    numbertextBox.Text = numberResultCmd;
                    foreach (string s in listResult)
                    {
                        resulttextBox.AppendText(s + Environment.NewLine);
                    }
                }
                catch (Exception ex)
                {

                    MessageBox.Show(ex.Message);
                }
                finally
                {
                    _sqlClient.disconnect();
                }
            }
        }
    }
}

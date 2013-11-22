using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
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
        private List<string> listResult;
        public Form1()
        {
            InitializeComponent();
            saveFileDialog1.Filter = "Text|*.txt";
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
                    listResult = new List<string>();
                    DateTime error_count_datetime = ToDateTime.Value.AddMinutes(0-double.Parse(min_comboBox.Text));
                    string numberResultCmd = @"SELECT count(temp) FROM
(
select DISTINCT public._gps_log._uid from public._gps_log
where
public._gps_log._time > '"+FromDateTime.Value.ToString("yyyy-MM-dd HH:mm")+@"'
AND
public._gps_log._time < '"+ToDateTime.Value.ToString("yyyy-MM-dd HH:mm")+@"'
) 
AS temp";
                    string errorNumberResultCmd = @"SELECT count(temp) FROM
(
select DISTINCT public._gps_log._uid from public._gps_log
where
public._gps_log._time > '" + FromDateTime.Value.ToString("yyyy-MM-dd HH:mm") + @"'
AND
public._gps_log._time < '" + error_count_datetime.ToString("yyyy-MM-dd HH:mm") + @"'
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
                    DataTable errorNumberResult = _sqlClient.get_DataTable(errorNumberResultCmd);
                    DataTable listResultDataTable = _sqlClient.get_DataTable(listResultCmd);
                    int numberInitial=0, errorNumber=0;
                    foreach (DataRow row in numberResult.Rows)
                    {
                        numberInitial = int.Parse(row[0].ToString());
                    }
                    foreach (DataRow row in errorNumberResult.Rows)
                    {
                        errorNumber = int.Parse(row[0].ToString());
                    }
                    foreach (DataRow VARIABLE in listResultDataTable.Rows)
                    {
                        listResult.Add(VARIABLE[0].ToString());
                    }
                    numbertextBox.Text = numberResultCmd=(numberInitial-errorNumber).ToString();
                    foreach (string s in listResult)
                    {
                        resulttextBox.AppendText(s.Replace("(","").Replace(")","")+ Environment.NewLine);
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

        private void ExportBtn_Click(object sender, EventArgs e)
        {
            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                StreamWriter sw = new StreamWriter(saveFileDialog1.FileName);
                sw.WriteLine(FromDateTime.Value.ToString("yyyy-MM-dd HH:mm") + " - " + ToDateTime.Value.ToString("yyyy-MM-dd HH:mm"));
                sw.WriteLine("number:" + numbertextBox.Text);
                sw.WriteLine("device list:");
                for (int i = 0; i < listResult.Count;i++)
                {
                    sw.WriteLine(listResult[i].Replace("(", "").Replace(")", ""));
                }
                sw.WriteLine("================================================");
                sw.Close();
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Timers;
using log4net;
using log4net.Config;
using System.Data;

namespace ConsoleApplication1_asyn_tcp_client
{
    internal class Program
    {
        private class SendPackage
        {
            public string uid;//gps_log._id
            public string deviceID;//gps_log._uid
            public string lat;//gps_log._lat
            public string lon;//gps_log._lon
            public string direction;//gps_log._course
            public string speed;//gps_log._speed
            public string deviceType;//B:船、C:車、A:人
            public string unitID;
            public string deviceStatus;//N:Normal、E:Emergency//gps_log.j_6
            public string systemSendTime;//server-time//gps_log._option1
            public string deviceSendTime;//info-time//gps_log._option0

            public SendPackage()
            {
                uid =
                    deviceID =
                        lat =
                            lon =
                                direction =
                                    speed =
                                        deviceType = unitID = deviceStatus = systemSendTime = deviceSendTime = "null";
            }
        }
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
       
        // State object for receiving data from remote device.
        public class StateObject
        {
            // Client socket.
            public Socket workSocket = null;
            // Size of receive buffer.
            public const int BufferSize = 256;
            // Receive buffer.
            public byte[] buffer = new byte[BufferSize];
            // Received data string.
            public StringBuilder sb = new StringBuilder();
        }

        public class AsynchronousClient
        {
            // The port number for the remote device.
            //private const int port = 11000;
            private static readonly IPAddress IpAddress = IPAddress.Parse(ConfigurationManager.AppSettings["THUNDER_SERVER_IP"]);

            private static readonly int Port = int.Parse(ConfigurationManager.AppSettings["THUNDE_SERVER_PORT"]);

            private static readonly SqlClient sql_client = new SqlClient(ConfigurationManager.AppSettings["SQL_SERVER_IP"], ConfigurationManager.AppSettings["SQL_SERVER_PORT"], ConfigurationManager.AppSettings["SQL_SERVER_USER_ID"], ConfigurationManager.AppSettings["SQL_SERVER_PASSWORD"], ConfigurationManager.AppSettings["SQL_SERVER_DATABASE"], ConfigurationManager.AppSettings["Pooling"], ConfigurationManager.AppSettings["MinPoolSize"], ConfigurationManager.AppSettings["MaxPoolSize"], ConfigurationManager.AppSettings["ConnectionLifetime"]);

            // ManualResetEvent instances signal completion.
            private static ManualResetEvent connectDone =
                new ManualResetEvent(false);

            private static ManualResetEvent sendDone =
                new ManualResetEvent(false);

            private static ManualResetEvent receiveDone =
                new ManualResetEvent(false);

            // The response from the remote device.
            private static String response = String.Empty;

            private static void StartClient(object sender, ElapsedEventArgs elapsedEventArgs)
            {
                // Connect to a remote device.
                try
                {
                    string timeNow = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    string prevNow = (DateTime.Parse(timeNow).AddSeconds(0 - double.Parse(ConfigurationManager.AppSettings["aTimer_interval_sec"]))).ToString("yyyy-MM-dd HH:mm:ss");

                    Console.WriteLine(timeNow);
                    Console.WriteLine(prevNow);
                            // Establish the remote endpoint for the socket.
                            // The name of the 
                            // remote device is "host.contoso.com".
                            //IPHostEntry ipHostInfo = Dns.Resolve("host.contoso.com");
                            //IPAddress ipAddress = ipHostInfo.AddressList[0];
                            //IPAddress ipAddress = IPAddress.Parse("");
                            IPEndPoint remoteEP = new IPEndPoint(IpAddress, Port);

                            // Create a TCP/IP socket.
                            Socket client = new Socket(AddressFamily.InterNetwork,
                               SocketType.Stream, ProtocolType.Tcp);
                            // Connect to the remote endpoint
                            client.BeginConnect(remoteEP,
                                new AsyncCallback(ConnectCallback), client);
                            connectDone.WaitOne();
                        
                        client.SendTimeout = client.ReceiveTimeout = 1000;
                        
                    //send package getting from sql command
                        /*
                         * 
                         * SELECT 
      public._gps_log._id,
      public._gps_log._uid,
      public._gps_log._lat,
      public._gps_log._lon,
      public._gps_log._course,
      public._gps_log._speed,
      sd.code.code_name,
      sd.unit.unit_name,
      public._gps_log.j_6,
      public._gps_log._option1,
      public._gps_log._option0
    FROM
      public._gps_log
      INNER JOIN sd.equipment ON (public._gps_log._uid = sd.equipment.uid)
      INNER JOIN sd.unit ON (sd.equipment.unit = (sd.unit.unit_id) ::varchar)
      INNER JOIN sd.code ON (sd.equipment.type = sd.code.code)
    WHERE
      public._gps_log._time > '2013-11-15 11:10:00' ::timestamp AND 
      public._gps_log._time < '2013-11-15 11:20:00' ::timestamp
                         */
                    string sqlCmd = @"
SELECT 
      public._gps_log._id,
      public._gps_log._uid,
      public._gps_log._lat,
      public._gps_log._lon,
      public._gps_log._course,
      public._gps_log._speed,
      sd.code.code_name,
      sd.unit.unit_name,
      public._gps_log.j_6,
      public._gps_log._option1,
      public._gps_log._option0
    FROM
      public._gps_log
      INNER JOIN sd.equipment ON (public._gps_log._uid = sd.equipment.uid)
      INNER JOIN sd.unit ON (sd.equipment.unit = (sd.unit.unit_id) ::varchar)
      INNER JOIN sd.code ON (sd.equipment.type = sd.code.code)
    WHERE
      public._gps_log._time > '"+prevNow+@"' ::timestamp 
    AND 
      public._gps_log._time < '" +timeNow+ @"' ::timestamp
";
                    sql_client.connect();
                    DataTable dt = sql_client.get_DataTable(sqlCmd);
                    sql_client.disconnect();
                    SendPackage sendPackage = new SendPackage();
                    string sendPackageStr = string.Empty;
                    if (dt != null && dt.Rows.Count != 0)
                    {
                        Console.WriteLine("+if");
                        Console.WriteLine("dt:{0}", dt);
                        Console.WriteLine("dt.Rows.Count:{0}", dt.Rows.Count);
                        foreach (DataRow row in dt.Rows)
                        {
                            sendPackage.uid = row[0].ToString();
                            sendPackage.deviceID = row[1].ToString();
                            sendPackage.lat = row[2].ToString();
                            sendPackage.lon = row[3].ToString();
                            sendPackage.direction = row[4].ToString();
                            sendPackage.speed = row[5].ToString();
                            sendPackage.deviceType = row[6].ToString();
                            sendPackage.unitID = row[7].ToString();
                            if (row[8].ToString() == "Emergency On")
                                sendPackage.deviceStatus = "Emergency";
                            else
                            {
                                sendPackage.deviceStatus = "Normal";
                            }
                            sendPackage.systemSendTime = row[9].ToString();
                            sendPackage.deviceSendTime = row[10].ToString();

                            sendPackageStr += sendPackage.uid +
                                            "," + sendPackage.deviceID +
                                            "," + sendPackage.lat +
                                            "," + sendPackage.lon +
                                            "," + sendPackage.direction +
                                            "," + sendPackage.speed +
                                            "," + sendPackage.deviceType +
                                            "," + sendPackage.unitID +
                                            "," + sendPackage.deviceStatus +
                                            "," + sendPackage.systemSendTime +
                                            "," + sendPackage.deviceSendTime +
                                            Environment.NewLine+
                                            "<EOF>";       
                        }
                    }
                        
                                      
                        // Send test data to the remote device.
                    Send(client, sendPackageStr);
                        sendDone.WaitOne();

                        // Receive the response from the remote device.
                        Receive(client);
                        receiveDone.WaitOne();

                        // Write the response to the console.
                        Console.WriteLine("Response received : {0}", response);

                        // Release the socket.
                        client.Shutdown(SocketShutdown.Both);
                        client.Close();
                    connectDone.Reset();
                    sendDone.Reset();
                    receiveDone.Reset();
                    if (!sendPackageStr.Equals("<EOF>"))
                        log.Info(sendPackageStr.Replace("<EOF>",""));



                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
            }

            private static void ConnectCallback(IAsyncResult ar)
            {
                try
                {
                    // Retrieve the socket from the state object.
                    Socket client = (Socket) ar.AsyncState;

                    // Complete the connection.
                    client.EndConnect(ar);

                    Console.WriteLine("Socket connected to {0}",
                        client.RemoteEndPoint.ToString());

                    // Signal that the connection has been made.
                    connectDone.Set();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
            }

            private static void Receive(Socket client)
            {
                 
                try
                {
                    // Create the state object.
                    StateObject state = new StateObject();
                    state.workSocket = client;

                    // Begin receiving the data from the remote device.
                    client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                        new AsyncCallback(ReceiveCallback), state);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
            }

            private static void ReceiveCallback(IAsyncResult ar)
            {
                try
                {
                    // Retrieve the state object and the client socket 
                    // from the asynchronous state object.
                    StateObject state = (StateObject) ar.AsyncState;
                    Socket client = state.workSocket;

                    // Read data from the remote device.
                    int bytesRead = client.EndReceive(ar);

                    if (bytesRead > 0)
                    {
                        // There might be more data, so store the data received so far.
                        state.sb.Append(Encoding.UTF8.GetString(state.buffer, 0, bytesRead));

                        // Get the rest of the data.
                        client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                            new AsyncCallback(ReceiveCallback), state);
                    }
                    else
                    {
                        // All the data has arrived; put it in response.
                        if (state.sb.Length > 1)
                        {
                            response = state.sb.ToString();
                        }
                        // Signal that all bytes have been received.
                        receiveDone.Set();
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
            }

            private static void Send(Socket client, String data)
            {
  
                // Convert the string data to byte data using ASCII encoding.
                byte[] byteData = Encoding.UTF8.GetBytes(data);

                // Begin sending the data to the remote device.
                client.BeginSend(byteData, 0, byteData.Length, 0,
                    new AsyncCallback(SendCallback), client);
            }

            private static void SendCallback(IAsyncResult ar)
            {
                try
                {
                    // Retrieve the socket from the state object.
                    Socket client = (Socket) ar.AsyncState;

                    // Complete sending the data to the remote device.
                    int bytesSent = client.EndSend(ar);
                    Console.WriteLine("Sent {0} bytes to server.", bytesSent);

                    // Signal that all bytes have been sent.
                    sendDone.Set();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
            }
            private static System.Timers.Timer aTimer;
            public static int Main(String[] args)
            {
                aTimer = new System.Timers.Timer(int.Parse(ConfigurationManager.AppSettings["aTimer_interval_sec"]) * 1000);
                aTimer.Elapsed += new ElapsedEventHandler(StartClient);
                aTimer.Enabled = true;
                Console.WriteLine("Press the Enter key to exit the program.");
                Console.ReadLine();
                //StartClient();
                return 0;
            }

        }
    }
}


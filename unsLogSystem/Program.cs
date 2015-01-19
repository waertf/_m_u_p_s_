using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Xml.Linq;
using keeplive;

namespace unsLogSystem
{
    class Program
    {
        public static Socket WavegisHandler;
        private static TcpClient unsTcpClient;
        static readonly string ipAddress = ConfigurationManager.AppSettings["MUPS_SERVER_IP"];
        static readonly int port = int.Parse(ConfigurationManager.AppSettings["MUPS_SERVER_PORT"]);
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private volatile static NetworkStream  unsNetworkStream;
        static Mutex _mutex = new Mutex(false, "unsLogSystem.exe");

        static void Main(string[] args)
        {
            if (!_mutex.WaitOne(1000, false))
                return;
            AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            ConnectToUnsServer();
            
            Thread wavegisToUnsThread = new Thread
              (delegate()
              {
                  WavegisToUnsListening();
              });
            wavegisToUnsThread.Start();
                Thread unsToWavegisThread = new Thread
              (delegate()
              {
                  unsToWavegis();
              });
                unsToWavegisThread.Start();
        }

        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var exception = e.ExceptionObject as Exception;
            if (exception != null)
            {
                log.Fatal("Restart:" + exception.ToString());
            }

            Environment.Exit(1);
        }

        static void CurrentDomain_ProcessExit(object sender, EventArgs e)
        {
            string logMsg = string.Empty;
            logMsg = "Close time:" + DateTime.Now.ToString("G") + Environment.NewLine +
                  "Memory usage:" +
                  Process.GetCurrentProcess().WorkingSet64 / 1024.0 / 1024.0;
            log.Fatal(logMsg);
            _mutex.ReleaseMutex();
        }

        private static void unsToWavegis()
        {
            while (true)
            {
                byte[] bytes;
                byte[] bytes_length = new byte[2];
                int numBytesRead = unsNetworkStream.Read(bytes_length, 0, bytes_length.Length);
                int data_length = GetLittleEndianIntegerFromByteArray(bytes_length, 0);
                bytes = new byte[data_length];
                int bytesRec = unsNetworkStream.Read(bytes, 0, bytes.Length);
                ThreadPool.QueueUserWorkItem(delegate
                {
                    string msg = null, logData=null;
                    msg = Encoding.ASCII.GetString(bytes, 0, bytes.Length);
                    XDocument xml_data = null;
                    xml_data = XDocument.Parse(msg);
                    logData = xml_data.ToString();
                    log.Info(logData);
                    Console.WriteLine(DateTime.UtcNow.ToString("G"));
                    Console.WriteLine(logData);
                    Console.WriteLine();
                });
                WavegisHandler.Send(Combine(bytes_length, bytes));
                Thread.Sleep(1);
            }
        }

        private static void ConnectToUnsServer()
        {
            Console.WriteLine("+unsConnectDone connect");
            unsTcpClient = new TcpClient();
            unsTcpClient.Connect(ipAddress, port);
            Console.WriteLine("-unsConnectDone connect");
            Keeplive.keep(unsTcpClient.Client);
            NetworkStream netStream = unsTcpClient.GetStream();
            unsNetworkStream = netStream;
        }

        private static void WavegisToUnsListening()
        {
            byte[] bytes;
            byte[] bytes_length;
            IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Any, port);
            // Create a TCP/IP socket.
            Socket listener = new Socket(AddressFamily.InterNetwork,
                SocketType.Stream, ProtocolType.Tcp);

            Console.WriteLine("Waiting for a connection...");
            listener.Bind(localEndPoint);
            listener.Listen(10);
            WavegisHandler = listener.Accept();
            Console.WriteLine("Waiting for data...");
            // Start listening for connections.
            while (true)
            {
                string data = null;
                bytes_length = new byte[2];
                int numBytesRead = WavegisHandler.Receive(bytes_length);
                int data_length = GetLittleEndianIntegerFromByteArray(bytes_length, 0);
                bytes = new byte[data_length];
                int bytesRec = WavegisHandler.Receive(bytes);
                unsNetworkStream.Write(Combine(bytes_length, bytes), 0, numBytesRead + bytesRec);
                Thread.Sleep(1);
            }
            
        }
        static int GetLittleEndianIntegerFromByteArray(byte[] data, int startIndex)
        {
            return (data[startIndex])
                 | (data[startIndex + 1] << 8);
            //| (data[startIndex + 2] << 8)
            //| data[startIndex + 3];
        }
        static byte[] Combine(params byte[][] arrays)
        {
            byte[] rv = new byte[arrays.Sum(a => a.Length)];
            int offset = 0;
            foreach (byte[] array in arrays)
            {
                Buffer.BlockCopy(array, 0, rv, offset, array.Length);
                offset += array.Length;
            }
            return rv;
        }
    }
}

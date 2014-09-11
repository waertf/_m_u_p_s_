using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using NetMQ;
using System.Management;
using System.ServiceProcess;
using System.Diagnostics;
using Gurock.SmartInspect;
namespace unsAtiaTrigger
{
    class Program
    {
        
        static void Main(string[] args)
        {
            SiAuto.Si.Enabled = true;
            SiAuto.Si.Level = Level.Debug;
            SiAuto.Si.Connections = @"file(filename=""" +
                                    Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) +
                                    "\\log.sil\",rotate=weekly,append=true,maxparts=5,maxsize=500MB)";
            //Console.WriteLine(RunCmd(Properties.Settings.Default.AddAtiaIpAdressAndSubnet));
            //Console.WriteLine(RunCmd(Properties.Settings.Default.RemoveAtiaIpAddress));
            using (NetMQContext context=NetMQContext.Create())
            {
                //changeIpAddressForUNS("192.168.1.28,192.168.1.27");
                Task serverTask = Task.Factory.StartNew(()=>Server(context));
                //Task clientTask = Task.Factory.StartNew(() => Client(context));
                //Task.WaitAll(serverTask, clientTask);
                Task.WaitAll(serverTask);
            }
        }
        static private string[] ip, subset, gateway, dns;
        private static string aitaNic = Properties.Settings.Default.ATIA_NIC_NAME;
        private static void changeIpAddressForATIA(string ipAddress)
        {
            // get index and nic name console command:wmic nic get index,name
            GetIP(aitaNic, out ip, out subset, out gateway, out dns);
            string ipS = string.Join(",", ip);
            string subsetS = subset[0];
            string gatewayS = string.Join(",", gateway);
            string dnsS = string.Join(",", dns);
            SetIP(aitaNic, ipAddress, subsetS, gatewayS, dnsS);
        }
        private static string unsNic = Properties.Settings.Default.UNS_NIC_NAME;
        private static void changeIpAddressForUNS(string ipAddress)
        {
            // get index and nic name console command:wmic nic get index,name
            GetIP(unsNic, out ip, out subset, out gateway, out dns);
            string ipS = string.Join(",", ip);
            string subsetS = subset[0];
            string gatewayS = string.Join(",", gateway);
            string dnsS = string.Join(",", dns);
            SetIP(unsNic, ipAddress, subsetS, gatewayS, dnsS);
        }

        private static void Client(string message)
        {
            using (NetMQContext context = NetMQContext.Create())
            using (NetMQSocket clientSocket = context.CreateRequestSocket())
            {
                clientSocket.Connect("tcp://"+Properties.Settings.Default.RemoteIpAddress+":"+Properties.Settings.Default.RemotePort);

                while (true)
                {
                    Console.WriteLine("Please enter your message:");
                    clientSocket.Send(message);

                    string answer = clientSocket.ReceiveString();

                    Console.WriteLine("Answer from server: {0}", answer);

                    if (answer == "exit")
                    {
                        break;
                    }
                }
            }
        }

        private static void Server(NetMQContext context)
        {
            using (NetMQSocket serverSocket = context.CreateResponseSocket())
            {
                serverSocket.Bind("tcp://*:5555");

                while (true)
                {
                    string message = serverSocket.ReceiveString();

                    Console.WriteLine("Receive message {0}", message);

                    switch (message)
                    {
                        case "CloseLocalAtiaThenStartRemoteAtia":
                            serverSocket.Send("exit");
                            //close atia
                            switch (Properties.Settings.Default.AtiaUnsServiceOrProcess)
                            {
                                case   "Service":
                                    StopService(Properties.Settings.Default.AtiaServiceName);
                                    break;
                                case "Process":
                                    KillProcess(Properties.Settings.Default.AtiaProcessName);
                                    break;
                            }
                            
                            //change ip address
                            //changeIpAddressForATIA(Properties.Settings.Default.BlockAtiaMsgIp);
                            //remove ip address
                            //System.Diagnostics.Process.Start("cmd.exe", "/C " + Properties.Settings.Default.RemoveAtiaIpAddress);
                            Console.WriteLine(RunCmd(Properties.Settings.Default.RemoveAtiaIpAddress));
                            //start remote atia
                            Client("StartRemoteAtia");
                            break;
                        case "StartRemoteAtia":
                            //change ip address
                            //changeIpAddressForATIA(Properties.Settings.Default.ReceiveAtiaMsgIp);
                            //add ip address
                            //System.Diagnostics.Process.Start("cmd.exe", "/C " + Properties.Settings.Default.AddAtiaIpAdressAndSubnet);
                            Console.WriteLine(RunCmd(Properties.Settings.Default.AddAtiaIpAdressAndSubnet));
                            switch (Properties.Settings.Default.AtiaUnsServiceOrProcess)
                            {
                                case "Service":
                                    StartService(Properties.Settings.Default.AtiaServiceName);
                                    break;
                                case "Process":
                                    StartProcess(Properties.Settings.Default.AtiaProcessPath,Properties.Settings.Default.AtiaProcessName);
                                    break;
                            }
                            serverSocket.Send("exit");
                            break;
                        case "CloseLocalUnsThenStartRemoteUns":
                            serverSocket.Send("exit");
                            //close uns
                            switch (Properties.Settings.Default.AtiaUnsServiceOrProcess)
                            {
                                case   "Service":
                                    StopService(Properties.Settings.Default.UnsServiceName);
                                    break;
                                case "Process":
                                    KillProcess(Properties.Settings.Default.UnsProcessName);
                                    break;
                            }
                            
                            //change ip address
                            //changeIpAddressForUNS(Properties.Settings.Default.BlockUnsMsgIp);
                            //start remote uns
                            Client("StartRemoteUns");
                            break;
                        case "StartRemoteUns":
                            //changeIpAddressForUNS(Properties.Settings.Default.ReceiveUnsMsgIp);
                            switch (Properties.Settings.Default.AtiaUnsServiceOrProcess)
                            {
                                case "Service":
                                    StartService(Properties.Settings.Default.UnsServiceName);
                                    break;
                                case "Process":
                                    StartProcess(Properties.Settings.Default.UnsProcessPath, Properties.Settings.Default.UnsProcessName);
                                    break;
                            }
                            serverSocket.Send("exit");
                            break;
                    }
                    /*
                    serverSocket.Send("World");

                    if (message == "exit")
                    {
                        break;
                    }
                    */
                }
            }      
        }

        private static void StartProcess(string ProcessPath, string ProcessName)
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo();
                psi.WorkingDirectory = @ProcessPath;
                psi.FileName = ProcessName;
                Process.Start(psi);
            }
            catch (Exception e)
            {

                Console.WriteLine(e.ToString());
                SiAuto.Main.LogException(e);
            }
        }

        private static void StartService(string ServiceName)
        {
            try
            {
                ServiceController controller = new ServiceController(ServiceName);
                controller.Start();

                controller.WaitForStatus(ServiceControllerStatus.Running);
                Console.WriteLine("Service status: " + controller.Status);
            }
            catch (Exception e)
            {

                Console.WriteLine(e.ToString());
                SiAuto.Main.LogException(e);
            }
        }

        private static void KillProcess(string ProcessName)
        {
            try
            {
                Process[] processes = Process.GetProcessesByName(ProcessName);
                foreach (Process process in processes)
                {
                    process.Kill();
                    process.WaitForExit();
                }
            }
            catch (Exception e)
            {
                
                Console.WriteLine(e.ToString());
                SiAuto.Main.LogException(e);
            }
        }

        private static void StopService(string ServiceName)
        {
            try
            {
                ServiceController controller = new ServiceController(ServiceName);
                controller.Stop();

                controller.WaitForStatus(ServiceControllerStatus.Stopped);
                Console.WriteLine("Service status: " + controller.Status);
            }
            catch (Exception e)
            {

                Console.WriteLine(e.ToString());
                SiAuto.Main.LogException(e);
            }
        }

        /// <summary>
        /// Set IP for the specified network card name
        /// </summary>
        /// <PARAM name="nicName">Caption of the network card</PARAM>
        /// <PARAM name="IpAddresses">Comma delimited string 
        ///           containing one or more IP</PARAM>
        /// <PARAM name="SubnetMask">Subnet mask</PARAM>
        /// <PARAM name="Gateway">Gateway IP</PARAM>
        /// <PARAM name="DnsSearchOrder">Comma delimited DNS IP</PARAM>
        public static void SetIP(string nicName, string IpAddresses,
          string SubnetMask, string Gateway, string DnsSearchOrder)
        {
            ManagementClass mc = new ManagementClass(
              "Win32_NetworkAdapterConfiguration");
            ManagementObjectCollection moc = mc.GetInstances();

            foreach (ManagementObject mo in moc)
            {
                // Make sure this is a IP enabled device. 
                // Not something like memory card or VM Ware
                if (mo["IPEnabled"] is bool ? (bool) mo["IPEnabled"] : false)
                {
                    string caption = null;
                    caption = mo["Caption"].ToString();
                    if (caption.Contains(nicName))
                    {

                        ManagementBaseObject newIP =
                          mo.GetMethodParameters("EnableStatic");
                        ManagementBaseObject newGate =
                          mo.GetMethodParameters("SetGateways");
                        ManagementBaseObject newDNS =
                          mo.GetMethodParameters("SetDNSServerSearchOrder");

                        newGate["DefaultIPGateway"] = new string[] { Gateway };
                        newGate["GatewayCostMetric"] = new int[] { 1 };

                        newIP["IPAddress"] = IpAddresses.Split(',');
                        newIP["SubnetMask"] = new string[] { SubnetMask };

                        newDNS["DNSServerSearchOrder"] = DnsSearchOrder.Split(',');

                        ManagementBaseObject setIP = mo.InvokeMethod(
                          "EnableStatic", newIP, null);
                        ManagementBaseObject setGateways = mo.InvokeMethod(
                          "SetGateways", newGate, null);
                        ManagementBaseObject setDNS = mo.InvokeMethod(
                          "SetDNSServerSearchOrder", newDNS, null);

                        break;
                    }
                }
            }
        }
        /// <summary>
        /// Returns the network card configuration of the specified NIC
        /// </summary>
        /// <PARAM name="nicName">Name of the NIC</PARAM>
        /// <PARAM name="ipAdresses">Array of IP</PARAM>
        /// <PARAM name="subnets">Array of subnet masks</PARAM>
        /// <PARAM name="gateways">Array of gateways</PARAM>
        /// <PARAM name="dnses">Array of DNS IP</PARAM>
        public static void GetIP(string nicName, out string[] ipAdresses,
          out string[] subnets, out string[] gateways, out string[] dnses)
        {
            ipAdresses = null;
            subnets = null;
            gateways = null;
            dnses = null;

            ManagementClass mc = new ManagementClass(
              "Win32_NetworkAdapterConfiguration");
            ManagementObjectCollection moc = mc.GetInstances();

            foreach (ManagementObject mo in moc)
            {
                // Make sure this is a IP enabled device. 
                // Not something like memory card or VM Ware
                if (mo["ipEnabled"] is bool ? (bool) mo["ipEnabled"] : false)
                {
                    string caption = null;
                    caption = mo["Caption"].ToString();
                    Console.WriteLine(caption);
                    if (caption.Contains(nicName))
                    {
                        ipAdresses = (string[])mo["IPAddress"];
                        subnets = (string[])mo["IPSubnet"];
                        gateways = (string[])mo["DefaultIPGateway"];
                        dnses = (string[])mo["DNSServerSearchOrder"];
                        break;
                    }
                }
            }
        }
        private static string RunCmd(string command)
         {
             //實例一個Process類，啟動一個獨立進程
             Process p = new Process();
 
             //Process類有一個StartInfo屬性，這個是ProcessStartInfo類，包括了一些屬性和方法，下面我們用到了他的幾個屬性：
 
             p.StartInfo.FileName = "cmd.exe";           //設定程序名
             p.StartInfo.Arguments = "/c " + command;    //設定程式執行參數
             p.StartInfo.UseShellExecute = false;        //關閉Shell的使用
             p.StartInfo.RedirectStandardInput = true;   //重定向標準輸入
             p.StartInfo.RedirectStandardOutput = true;  //重定向標準輸出
             p.StartInfo.RedirectStandardError = true;   //重定向錯誤輸出
             p.StartInfo.CreateNoWindow = true;          //設置不顯示窗口
 
             p.Start();   //啟動
             
             //p.StandardInput.WriteLine(command);       //也可以用這種方式輸入要執行的命令
             //p.StandardInput.WriteLine("exit");        //不過要記得加上Exit要不然下一行程式執行的時候會當機
            
             return p.StandardOutput.ReadToEnd();        //從輸出流取得命令執行結果
 
         }
    }
}

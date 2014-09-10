using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetMQ;
using System.Management;
namespace unsAtiaTrigger
{
    class Program
    {
        
        static void Main(string[] args)
        {
            using (NetMQContext context=NetMQContext.Create())
            {
                changeIpAddress(Properties.Settings.Default.ReceiveMsgIp);
                Task serverTask = Task.Factory.StartNew(()=>Server(context));
                //Task clientTask = Task.Factory.StartNew(() => Client(context));
                //Task.WaitAll(serverTask, clientTask);
                Task.WaitAll(serverTask);
            }
        }
        static private string[] ip, subset, gateway, dns;
        private static string nic = Properties.Settings.Default.NIC_NAME;
        private static void changeIpAddress(string ipAddress)
        {
            // get index and nic name console command:wmic nic get index,name
            GetIP(nic, out ip, out subset, out gateway, out dns);
            string ipS = string.Join(",", ip);
            string subsetS = subset[0];
            string gatewayS = string.Join(",", gateway);
            string dnsS = string.Join(",", dns);
            SetIP(nic, ipAddress, subsetS, gatewayS, dnsS);
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

                    if (message == "exit")
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
                            //change ip address
                            //start remote atia
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
    }
}

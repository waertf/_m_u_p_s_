using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NetMQ;

namespace netmq_test
{
    class Program
    {
        static void Main(string[] args)
        {
            using (NetMQContext context = NetMQContext.Create())
            {
                using (var server = context.CreateResponseSocket())
                {
                    //server.Bind("tcp://127.0.0.1:5556");
                    using (NetMQSocket clientSocket = context.CreateRequestSocket())
                    {
                        clientSocket.Connect("tcp://127.0.0.1:5556");
                        clientSocket.Send("ping",true,false);
                        server.Send();
                        Console.WriteLine(DateTime.Now);
                        var poller = clientSocket.Poll(new TimeSpan(0, 0, 5));
                        Console.WriteLine(poller);
                        Console.WriteLine(DateTime.Now);
                    }
                }
                
            }
            

            End();
        }

        private static void End()
        {
            Console.WriteLine("Press any key to exit...");
            Console.ReadLine();
        }
    }
}

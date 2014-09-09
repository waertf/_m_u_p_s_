using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetMQ;
namespace unsAtiaTrigger
{
    class Program
    {
        static void Main(string[] args)
        {
            using (NetMQContext context=NetMQContext.Create())
            {
                Task serverTask = Task.Factory.StartNew(()=>Server(context));
                Task clienTask = Task.Factory.StartNew(() => Client(context));
                Task.WaitAll(serverTask, clienTask);
            }
        }

        private static void Client(NetMQContext context)
        {
            using (NetMQSocket clientSocket = context.CreateRequestSocket())
            {
                clientSocket.Connect("tcp://127.0.0.1:5555");

                while (true)
                {
                    Console.WriteLine("Please enter your message:");
                    string message = Console.ReadLine();
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

                    serverSocket.Send("World");

                    if (message == "exit")
                    {
                        break;
                    }
                }
            }      
        }
    }
}

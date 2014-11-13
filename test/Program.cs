using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using NetMQ;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace test
{
    class Program
    {
        static void Main(string[] args)
        {
            //UnSubscribe();
            var factory = new ConnectionFactory() { HostName = "localhost" };
            using (var connection = factory.CreateConnection())
            {
                using (var channel = connection.CreateModel())
                {
                    channel.ExchangeDeclare("logs", "fanout");

                    var queueName = channel.QueueDeclare().QueueName;

                    channel.QueueBind(queueName, "logs", "");
                    var consumer = new QueueingBasicConsumer(channel);
                    channel.BasicConsume(queueName, true, consumer);

                    Console.WriteLine(" [*] Waiting for logs." +
                                      "To exit press CTRL+C");
                    while (true)
                    {
                        var ea = (BasicDeliverEventArgs)consumer.Queue.Dequeue();

                        var body = ea.Body;
                        var message = Encoding.UTF8.GetString(body);
                        Console.WriteLine(" [x] {0}", message);
                    }
                }
            }
            Console.WriteLine("Press any key to exit");
            Console.ReadLine();
        }
        private static string GetMessage(string[] args)
        {
            return ((args.Length > 0) ? string.Join(" ", args) : "info: Hello World!");
        }

        static void UnSubscribe()
        {
            using (NetMQContext contex = NetMQContext.Create())
            {
                using (var pub = contex.CreatePublisherSocket())
                {
                    pub.Bind("tcp://127.0.0.1:5002");

                    using (var sub = contex.CreateSubscriberSocket())
                    {
                        sub.Connect("tcp://127.0.0.1:5002");
                        sub.Subscribe("A");

                        // let the subscrbier connect to the publisher before sending a message
                        Thread.Sleep(500);

                        pub.SendMore("A");
                        pub.Send("Hello");

                        bool more;

                        string m = sub.ReceiveString(out more);

                        Console.WriteLine(m);
                        Console.WriteLine(more);

                        string m2 = sub.ReceiveString(out more);

                        Console.WriteLine(m2);
                        Console.WriteLine(more);
                        sub.Unsubscribe("A");

                        Thread.Sleep(500);

                        pub.SendMore("A");
                        pub.Send("Hello");

                        //string m3 = sub.ReceiveString(true, out more);
                        //Console.WriteLine(m3);
                    }
                }
            }
        }
        static void NotSubscribed()
        {
            using (NetMQContext contex = NetMQContext.Create())
            {
                using (var pub = contex.CreatePublisherSocket())
                {
                    pub.Bind("tcp://127.0.0.1:5002");

                    using (var sub = contex.CreateSubscriberSocket())
                    {
                        sub.Connect("tcp://127.0.0.1:5002");
                        sub.Subscribe("e");
                        // let the subscrbier connect to the publisher before sending a message
                        Thread.Sleep(500);

                        pub.Send("Hello");

                        bool more;

                        string m = sub.ReceiveString(true, out more);
                        Console.WriteLine(m);
                    }
                }
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace timer_test
{
    class Program
    {
        static void Main(string[] args)
        {
            var autoSendFromSqlTableTimer =
                    new System.Timers.Timer(5000);
            autoSendFromSqlTableTimer.Elapsed += (sender, e) => { AutoSend(DateTime.Now); };
            autoSendFromSqlTableTimer.Enabled = true;
            Console.ReadLine();
        }

        static void AutoSend(DateTime dt)
        {
            Console.WriteLine(dt);
        }

    }
}

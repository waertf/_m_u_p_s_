using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Timers;
using log4net;
using log4net.Appender;
using log4net.Config;

namespace ConsoleApplication1LogEveryXMinuteTest
{
    class Program
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private static System.Timers.Timer aTimer = new System.Timers.Timer(1*1000);
        static void Main(string[] args)
        {
            AutoResetEvent autoEvent = new AutoResetEvent(false);
            aTimer.Elapsed += new ElapsedEventHandler(aTimer_Elapsed);
            aTimer.Enabled = true;
            autoEvent.WaitOne();

        }

        static void aTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            log.Error("test");
            Console.WriteLine(DateTime.Now);
        }
    }
    
}

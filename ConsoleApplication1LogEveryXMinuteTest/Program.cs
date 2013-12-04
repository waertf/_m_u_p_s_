using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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
            string outputDirectoryName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string firstFolder = DateTime.Now.ToString("yyyyMMdd");
            string secondFolder = DateTime.Now.ToString("HH");
            string fileName = outputDirectoryName + "\\" + firstFolder + "\\" + secondFolder+"\\"+DateTime.Now.ToString("HHmmss") + ".txt";
            System.IO.Directory.CreateDirectory(outputDirectoryName + "\\" + firstFolder + "\\" + secondFolder);
            if (!File.Exists(fileName))
            {
                // Create a file to write to.
                using (StreamWriter sw = File.CreateText(fileName))
                {
                    sw.WriteLine("Hello");
                    sw.WriteLine("And");
                    sw.WriteLine("Welcome");
                }
            }
            //log.Error("test");
            Console.WriteLine(DateTime.Now);
        }
    }
    
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

class TimerExample
{
    static int invokeCount = 0;

    static void Main()
    {
        AutoResetEvent autoEvent = new AutoResetEvent(false);
        // Create the delegate that invokes methods for the timer.
        TimerCallback timerDelegate =
            new TimerCallback(CheckStatus);
        Timer stateTimer =
            new Timer(timerDelegate, autoEvent, 0,  1000);
        autoEvent.WaitOne(-1);
    }
    public static void CheckStatus(Object stateInfo)
    {
        Console.WriteLine("{0} Checking status {1,2}.",
            DateTime.Now.ToString("h:mm:ss.fff"),
            (++invokeCount).ToString());

    }
}



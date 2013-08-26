using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ConsoleApplication1_test2
{
    class Program
    {
        public struct test
        {
            public string t1;
            public string t2;
        }
        static void Main(string[] args)
        {

            DateTime dt = DateTime.Now;
            string now = string.Format("{0:yyyyMMdd hh:mm:ss.fff}", dt);
            Console.WriteLine(now);
            test struct_test = new test();
            struct_test.t1 = "test";
            Console.WriteLine(struct_test.t1);
            Console.ReadLine();
        }
    }
}

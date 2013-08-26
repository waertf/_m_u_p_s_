using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ConsoleApplication1_test
{
    class Program
    {
        static void Main(string[] args)
        {
            //byte[] data = { 0xd1 ,0x06};//1745
            byte[] data = { 0x5e, 0x00 };//94
           // Array.Reverse(data);
            //ing value = BitConverter.ToUInt64(data, 0);
            int value = GetLittleEndianIntegerFromByteArray(data, 0);
            Console.WriteLine("value:{0}", value);
            Console.Read();
        }
        static int GetLittleEndianIntegerFromByteArray(byte[] data, int startIndex)
        {
            return (data[startIndex] )
                 | (data[startIndex + 1] << 8);
                 //| (data[startIndex + 2] << 8)
                 //| data[startIndex + 3];
        }


    }
}

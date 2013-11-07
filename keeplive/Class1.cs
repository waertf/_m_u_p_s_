using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace keeplive
{
    public class Keeplive
    {
        /// <summary>
        /// send keepalive package to monitor if connection fail.
        /// if yes, when try to READ package there is an exception occured.
        /// </summary>
        /// <param name="Socket"></param>
        public static void keep(System.Net.Sockets.Socket Socket)
        {
            SetKeepAliveValues(Socket, true, 5000, 1000);
        }
        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Explicit)]
        struct TcpKeepAlive
        {
            [System.Runtime.InteropServices.FieldOffset(0)]
            private unsafe fixed byte Bytes[12];

            [System.Runtime.InteropServices.FieldOffset(0)]
            public uint On_Off;
            [System.Runtime.InteropServices.FieldOffset(4)]
            public uint KeepALiveTime;
            [System.Runtime.InteropServices.FieldOffset(8)]
            public uint KeepALiveInterval;

            public byte[] ToArray()
            {
                unsafe
                {
                    fixed (byte* ptr = Bytes)
                    {
                        IntPtr p = new IntPtr(ptr);
                        byte[] BytesArray = new byte[12];

                        System.Runtime.InteropServices.Marshal.Copy(p, BytesArray, 0, BytesArray.Length);
                        return BytesArray;
                    }
                }
            }
        }
        private static int SetKeepAliveValues
    (
        System.Net.Sockets.Socket Socket,
        bool On_Off,
        uint KeepaLiveTime,
        uint KeepaLiveInterval
    )
        {
            int Result = -1;

            unsafe
            {
                TcpKeepAlive KeepAliveValues = new TcpKeepAlive();

                KeepAliveValues.On_Off = Convert.ToUInt32(On_Off);
                KeepAliveValues.KeepALiveTime = KeepaLiveTime;
                KeepAliveValues.KeepALiveInterval = KeepaLiveInterval;

                byte[] InValue = new byte[12];

                //for (int I = 0; I < 12; I++)
                //InValue[I] = KeepAliveValues.Bytes[I];
                Array.Copy(KeepAliveValues.ToArray(), InValue, InValue.Length);

                Result = Socket.IOControl(System.Net.Sockets.IOControlCode.KeepAliveValues, InValue, null);
            }

            return Result;
        }
    }
}

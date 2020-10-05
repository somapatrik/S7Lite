using Snap7;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace S7Lite
{
    public static class ThisPlc
    {
        public static S7Server PLC;

        public static string PLC_IP = "127.0.0.1";

        public static List<byte[]> PLC_Memory;

        public static Boolean StartPLCServer()
        {
            PLC = new S7Server();

            return PLC.StartTo(PLC_IP) == 0 ? true : false;
        }
    }
}

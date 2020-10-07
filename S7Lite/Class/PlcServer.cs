using S7Lite.Class;
using Snap7;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace S7Lite
{
    public static class PlcServer
    {
        public static S7Server PLC = new S7Server();

        public static string PLC_IP;

        public static List<DB> PLC_Memory;

        public static bool IsRunning;

        public static Boolean StartPLCServer()
        {
            bool error = PLC.StartTo(PLC_IP) == 0 ? false : true;
            IsRunning = error ? false : true;
            return IsRunning;
        }

        public static void StopPLCServer()
        {
            PLC.Stop();
            PLC.CpuStatus = 4;
            IsRunning = false;
        }

        private static void RegisterDB()
        {
            foreach(DB datablock in PLC_Memory)
            {
                PLC.RegisterArea(S7Server.S7AreaDB, datablock.number, ref datablock.array, datablock.array.Length);
            }
        }
    }
}

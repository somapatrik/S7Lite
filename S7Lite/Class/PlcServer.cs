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
        public static S7Server PLC; //= new S7Server();

        public static string PLC_IP;

        public static List<DB> PLC_Memory = new List<DB>();

        public static bool IsRunning;


        public static Boolean StartPLCServer()
        {
            if (PLC == null)
                PLC = new S7Server();

            bool error = PLC.StartTo(PLC_IP) == 0 ? false : true;
            IsRunning = error ? false : true;

            if (IsRunning)
                PLC.CpuStatus = 8;

            return IsRunning;
        }

        public static void StopPLCServer()
        {
            PLC.Stop();
            PLC.CpuStatus = 4;
            IsRunning = false;
        }

        public static void AddDB(ref DB newdb)
        {
            PLC_Memory.Add(newdb);

        }

        private static void RegisterDB()
        {
            foreach(DB datablock in PLC_Memory)
            {
                PLC.RegisterArea(S7Server.S7AreaDB, datablock.number, ref datablock.array, datablock.array.Length);
            }
        }

        public static int GetAvailableDB()
        {
            int ret = 1;

            if (PLC_Memory.Count > 0)
            {
                for (int i = 1; i <= 1024; i++)
                {
                    if (!PLC_Memory.Exists(o => o.number == i))
                    {
                        return i;
                    }
                }
                ret = 0;
            }
            return ret;            
        }

        public static bool IsDbAvailable(int num)
        {
            return !PLC_Memory.Exists(o => o.number == num );
        }
    }
}

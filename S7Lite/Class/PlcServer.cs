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
        public static List<DB> PLC_Memory = new List<DB>();
        //public static bool IsRunning;
        public static string PLC_IP;

        public static int MaxDBCount = 1024;

        public static S7Server.TSrvCallback PlcCallBack = new S7Server.TSrvCallback(PlcEventCallBack);

        public static event EventHandler UpdatedDB;

        public static int CPUStatus { get { return PLC.CpuStatus; }  }
        public static int ServerStatus { get { return PLC.ServerStatus; } }
        public static bool IsRunning { get { return PLC.ServerStatus == 1 ? true : false; }   }

        static void PlcEventCallBack(IntPtr usrPtr, ref S7Server.USrvEvent Event, int Size)
        {
            // New data on server
            if ((Event.EvtCode == S7Server.evcDataWrite) &&             
                (Event.EvtRetCode == 0) &&                  // No error
                (Event.EvtParam1 == S7Server.S7AreaDB))     // Is DB event
            {
                EventHandler handler = UpdatedDB;
                if (handler != null)
                {
                    handler(Event.EvtParam2,null);
                }
            }
        }

        public static void IniServer()
        {
            //PlcCallBack = new S7Server.TSrvCallback(PlcEventCallBack);
            //byte[] field = new byte[256];
            //PLC.RegisterArea(S7Server.S7AreaDB, 1, ref field, field.Length);
            PLC.SetEventsCallBack(PlcCallBack, IntPtr.Zero);
            PLC.CpuStatus = 4;
        }

        public static bool StartPLCServer()
        {
           RegisterDB();
            bool error = PLC.StartTo(PLC_IP) == 0 ? false : true;

            if (IsRunning)
                PLC.CpuStatus = 8;

            return IsRunning;
        }

        public static void StopPLCServer()
        {
            PLC.Stop();
            PLC.CpuStatus = 4;
            UnregisterAllDB();
        }

        #region DB add/remove

        public static void AddDB(ref DB newdb)
        {
            PLC_Memory.Add(newdb);
        }

        public static void RegisterDB()
        {
            foreach(DB datablock in PLC_Memory)
            {
                PLC.RegisterArea(S7Server.srvAreaDB, datablock.number, ref datablock.array, datablock.array.Length);
            }
        }

        private static void UnregisterDB(int num)
        {
            if (PLC!=null)
                PLC.UnregisterArea(S7Server.S7AreaDB, num);
        }

        public static void UnregisterAllDB()
        {
            foreach (DB datablock in PLC_Memory)
            {
                PLC.UnregisterArea(S7Server.srvAreaDB, datablock.number);
            }
        }

        public static void DBRemove(int DBNumber)
        {
            UnregisterDB(DBNumber);
            DBRemove(PLC_Memory.Find(o => o.number == DBNumber));
        }

        public static void DBRemove(DB db)
        {
            PLC_Memory.Remove(db);
        }

        #endregion

        #region DB available / search

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
            return (!PLC_Memory.Exists(o => o.number == num )) & 
                (num >= 1 & num <= MaxDBCount);
        }

        #endregion
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace S7Lite
{
    public static class Logger
    {

        public static readonly object LogLock = new object() ;

        public enum LogState { Normal, Error, Warning};

        public static void Log(string msg, LogState msgstate = LogState.Normal)
        {

           lock(LogLock) 
            {
                Directory.CreateDirectory("Log");
                CheckLog();

                string state = "";

                switch (msgstate)
                {
                    case LogState.Error:
                        state = "[  ERROR  ]";
                        break;
                    case LogState.Warning:
                        state = "[ WARNING ]";
                        break;
                    default:
                        state = "[         ]";
                        break;
                }
                 
                string msgline = DateTime.Now.ToString("HH:mm:ss") + " " + state + " " + msg;

                File.AppendAllText(Path.Combine("Log",DateTime.Now.ToString("yyMMdd")) + ".txt", msgline + Environment.NewLine);
            }

        } 

        private static async void CheckLog()
        {
            await new Task(new Action(() =>
            {
                string filename = Path.Combine("Log", DateTime.Now.ToString("yyMMdd") + ".txt");

                if (!File.Exists(filename))
                {
                    File.Create(filename);
                }
            }));
        }
       
    }
}

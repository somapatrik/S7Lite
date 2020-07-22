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

        public static void Logi(string msg)
        {

           lock(LogLock) {
                
                    if (!File.Exists("log.txt"))
                    {
                        File.Create("log.txt");
                    }

                    File.AppendAllText("log.txt", msg + Environment.NewLine);

                    
            }
            
        } 
    }
}

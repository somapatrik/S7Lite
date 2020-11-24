using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace S7Lite.Class
{
    public class DB
    {

        public int number;
        public string name;
        public byte[] array;

        public DB(int number, byte[] array, string name = "")
        {
            this.number = number;
            this.array = array;
            this.name = name;
        }
    }
}

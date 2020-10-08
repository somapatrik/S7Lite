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
        public byte[] array;

        public DB(int number, ref byte[] array)
        {
            this.number = number;
            this.array = array;
        }
    }
}

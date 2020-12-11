using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace S7Lite.Class
{
    class ByteIndex
    {
        public int Index;
        public int Byte;

        public ByteIndex() { }
        public ByteIndex(int index, int bytenumber)
        {
            Index = index;
            Byte = bytenumber;
        }

    }
}

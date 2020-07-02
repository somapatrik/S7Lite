using Snap7;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace S7Lite
{
    class PlcValue
    {
        private Boolean _IsBit;
        private Boolean _IsByte;
        private Boolean _IsWord;
        private Boolean _IsDouble;

        private Boolean _IsDbBit;
        private Boolean _IsDbByte;
        private Boolean _IsDbWord;
        private Boolean _IsDbDouble;

        private Boolean _IsInput;
        private Boolean _IsOutput;
        private Boolean _IsM;
        private Boolean _IsDb;

        private int _BufferSize;
        private int _StartAddress;
        private int _BitPosition;
        private int _DBNumber;
        private int _WordLen;
        private int _Area;

        private string _Displayname;

        private Boolean _IsValid;
        private byte[] _Value;
        private string _Address;


        public string Displayname
        {
            get { return _Displayname; }
            set { _Displayname = value; }
        }

        public int Area
        {
            get
            {
                return _Area;
            }
        }

        public int WordLen
        {
            get { return _WordLen; }
        }

        public int DBNumber
        {
            get
            {
                return _DBNumber;
            }
        }

        public int BitPosition
        {
            get { return _BitPosition; }
        }

        public int StartAddress
        {
            get { return _StartAddress; }
        }

        public int BufferSize
        {
            get { return _BufferSize; }
        }

        public Boolean IsInput
        {
            get { return _IsInput; }
        }

        public Boolean IsOutput
        {
            get { return _IsOutput; }
        }

        public Boolean IsM
        {
            get { return _IsM; }
        }

        public Boolean IsDB
        {
            get { return _IsDb; }
        }

        public Boolean IsBit
        {
            get
            {
                return _IsBit | _IsDbBit;
            }
        }

        public Boolean IsByte
        {
            get
            {
                return _IsByte | _IsDbByte;
            }
        }

        public Boolean IsWord
        {
            get
            {
                return _IsWord | _IsDbWord;
            }
        }

        public Boolean IsDouble
        {
            get
            {
                return _IsDouble | _IsDbDouble;
            }
        }

        public PlcValue()
        {

        }

        public PlcValue(string address)
        {
            this.Address = address;
            this._Displayname = "";
        }

        public PlcValue(string address, string name)
        {
            this.Address = address;
            this._Displayname = name;
        }

        public Boolean IsValid
        {
            get
            {
                return _IsValid;
            }
        }

        public byte[] ByteValue
        {
            set
            {
                _Value = value;
            }
        }

        public string Address
        {
            set
            {
                string ForAd = value.ToUpper().Trim().Replace(" ", "").Replace(",", ".");
                _Address = ForAd;
                CheckAddr();
                if (_IsValid)
                {
                    CheckType();
                    SetBufferSize();
                    SetStart();
                    if (_IsBit)
                    {
                        SetBitPosition();
                        _StartAddress += _BitPosition;
                    }
                    if (_IsDb)
                    {
                        SetDBNumber();
                    }
                    SetWordLen();

                }
            }

            get { return _Address; }
        }

        private void SetWordLen()
        {
            if (_IsBit)
            {
                _WordLen = S7Consts.S7WLBit;
            }
            else
            {
                _WordLen = BufferSize == 1 ? S7Consts.S7WLByte : 0;
                _WordLen = BufferSize == 2 ? S7Consts.S7WLWord : WordLen;
                _WordLen = BufferSize == 4 ? S7Consts.S7WLDWord : WordLen;
            }
        }

        private void SetDBNumber()
        {
            _DBNumber = Int32.Parse(_Address.Split('.')[0].Substring(2));
        }

        private void SetBitPosition()
        {
            if (!_IsDb)
            {
                // I99.[7]
                _BitPosition = Int32.Parse(_Address.Split('.')[1]);
            }
            else
            {
                // DB99.DBX99.[7]
                _BitPosition = Int32.Parse(_Address.Split('.')[2]);
            }
        }

        private void SetStart()
        {
            if (!_IsBit)
            {
                if (!_IsDb)
                {
                    // IB[32]
                    _StartAddress = Int32.Parse(_Address.Substring(2));
                }
                else
                {
                    // DB99.DBB[32]
                    _StartAddress = Int32.Parse(_Address.Split('.')[1].Substring(3));
                }

            }
            else
            {
                // When reading bit I need start in bits not bytes
                if (!_IsDb)
                {
                    // I[99].7 => 99 bytes = 99 * 8 bits
                    _StartAddress = (Int32.Parse(_Address.Split('.')[0].Substring(1)) * 8);
                }
                else
                {
                    // DB99.DBX[32].7 => 32 bytes = 32 * 8 bits
                    _StartAddress = (Int32.Parse(_Address.Split('.')[1].Substring(3)) * 8);
                }
            }
        }

        private void SetBufferSize()
        {
            int val = 0;

            if (_IsBit)
            {
                val = 1;
            }
            else
            {

                string ReadLetter;

                if (!_IsDb)
                {
                    // I[B], I[W], I[D]
                    ReadLetter = _Address.Substring(1, 1);
                }
                else
                {
                    // DB100.DB[B]1
                    ReadLetter = _Address.Split('.')[1].Substring(2, 1);
                }

                if (ReadLetter == "B") { val = 1; }
                if (ReadLetter == "W") { val = 2; }
                if (ReadLetter == "D") { val = 4; }
            }

            _BufferSize = val;
        }

        private void CheckType()
        {
            _IsInput = _Address.StartsWith("I");
            _IsOutput = _Address.StartsWith("Q");
            _IsM = _Address.StartsWith("M");
            _IsDb = _Address.StartsWith("DB");

            if (_IsInput) { _Area = S7Consts.S7AreaPE; }
            if (_IsOutput) { _Area = S7Consts.S7AreaPA; }
            if (_IsM) { _Area = S7Consts.S7AreaMK; }
            if (_IsDb) { _Area = S7Consts.S7AreaDB; }
        }

        private void CheckAddr()
        {
            Regex NormalBit = new Regex(@"[I,Q,M]\d+[.][0-7]$", RegexOptions.IgnoreCase);
            Regex NormalByte = new Regex(@"[I,Q,M][B]\d+$", RegexOptions.IgnoreCase);
            Regex NormalWord = new Regex(@"[I,Q,M][W]\d+$", RegexOptions.IgnoreCase);
            Regex NormalDouble = new Regex(@"[I,Q,M][D]\d+$", RegexOptions.IgnoreCase);

            Regex DBBit = new Regex(@"\DB\d+.DBX\d+[.][0-7]$", RegexOptions.IgnoreCase);
            Regex DBByte = new Regex(@"\DB\d+.DB[B]\d+$", RegexOptions.IgnoreCase);
            Regex DBWord = new Regex(@"\DB\d+.DB[W]\d+$", RegexOptions.IgnoreCase);
            Regex DBDouble = new Regex(@"\DB\d+.DB[D]\d+$", RegexOptions.IgnoreCase);

            if (NormalBit.IsMatch(_Address)) { _IsBit = true; _IsValid = true; return; }
            if (NormalByte.IsMatch(_Address)) { _IsByte = true; _IsValid = true; return; }
            if (NormalWord.IsMatch(_Address)) { _IsWord = true; _IsValid = true; return; }
            if (NormalDouble.IsMatch(_Address)) { _IsDouble = true; _IsValid = true; return; }

            if (DBBit.IsMatch(_Address)) { _IsDbBit = true; _IsValid = true; return; }
            if (DBByte.IsMatch(_Address)) { _IsDbByte = true; _IsValid = true; return; }
            if (DBWord.IsMatch(_Address)) { _IsDbWord = true; _IsValid = true; return; }
            if (DBDouble.IsMatch(_Address)) { _IsDbDouble = true; _IsValid = true; return; }
        }

    }
}

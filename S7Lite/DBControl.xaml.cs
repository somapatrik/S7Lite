using S7Lite.Class;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Snap7;

namespace S7Lite
{
    public partial class DBControl : UserControl
    {
        List<int> UsedIndexes = new List<int>();
        List<int> DBUsedBytes = new List<int>();

        // Max DBSize
        int DBSize = 1024;

        // DB #
        public int DBNumber;

        //DB obj
        DB DBobject;

        // DB memory
        byte[] datablock;

        // Combo items
        List<string> combotypes = new List<string> { "BIT", "BYTE", "CHAR", "WORD", "INT", "DWORD", "DINT", "REAL" };

        public event EventHandler DBRightClicked;

        public DBControl(ref DB db)
        {
            InitializeComponent();
            DBobject = db;
            DBNumber = db.number;
            datablock = db.array;
            lblDBName.Content = db.name;

            SetGui();
        }

        public void Activate()
        {
            DisableAddresses();
            DisableCombos();
            DisableActValues();
        }

        public void Deactivate()
        {
            EnableAddresses();
            EnableCombos();
            EnableActValues();
        }


        public async void UpdateDB()
        {
            await Task.Run(() =>
            {
                GridData.Dispatcher.Invoke(() =>
                {
                    foreach (int i in UsedIndexes)
                    {
                        ReadValue(i);  
                    }
                });
            });
        }

        public async void WriteAll()
        {
            await Task.Run(() =>
            {
                GridData.Dispatcher.Invoke(() =>
                {
                    foreach (int i in UsedIndexes)
                    {
                        WriteValue(i);
                    }
                });
            });
        }

        public void WriteValue(int index)
        {
            // Get address
            int address = Int32.Parse(GetTextBox("blcaddress_" + index).Text);
            // Get value to write
            string input = GetTextBox("inputvalue_" + index).Text;
            // Type
            string type = GetComboBox("cmbtype_" + index).SelectedValue.ToString();

            switch (type)
            {
                case "BIT":
                    break;
                case "BYTE":
                    S7.SetByteAt(datablock, address, byte.Parse(input));
                    break;
                case "CHAR":
                    S7.SetCharsAt(datablock, address, input[0].ToString());
                    break;
                case "INT":
                    S7.SetIntAt(datablock, address, short.Parse(input));
                    break;
                case "WORD":
                    S7.SetWordAt(datablock, address, ushort.Parse(input));
                    break;
                case "DINT":
                    S7.SetDIntAt(datablock, address, int.Parse(input));
                    break;
                case "DWORD":
                    S7.SetDWordAt(datablock, address, uint.Parse(input));
                    break;
                case "REAL":
                    S7.SetRealAt(datablock, address, float.Parse(input));
                    break;
            }
            ReadValue(index);
        }

        private void ReadValue(int index)
        {
            // Get address
            int address = Int32.Parse(GetTextBox("blcaddress_" + index).Text);
            // Get output box
            TextBox output = GetTextBox("txtvalue_" + index);
            // Type
            string type = GetComboBox("cmbtype_" + index).SelectedValue.ToString();

            switch (type)
            {
                case "BIT":
                    break;
                case "BYTE":
                    output.Text = S7.GetByteAt(datablock, address).ToString();
                    break;
                case "CHAR":
                    output.Text = S7.GetCharsAt(datablock, address, 1);
                    break;
                case "INT":
                    output.Text = S7.GetIntAt(datablock, address).ToString();
                    break;
                case "WORD":
                    output.Text = S7.GetWordAt(datablock, address).ToString();
                    break;
                case "DINT":
                    output.Text = S7.GetDIntAt(datablock, address).ToString();
                    break;
                case "DWORD":
                    output.Text = S7.GetDWordAt(datablock, address).ToString();
                    break;
                case "REAL":
                    output.Text = S7.GetRealAt(datablock, address).ToString();
                    break;
            }
        }

        #region Utils

        private void SetGui()
        {
            AddRow();
            lblUsedBytes.Content = "";
            lblDBNumber.Content = "DB " + DBNumber;
        }

        private void SetUsedBytes()
        {
            lblUsedBytes.Content = DBUsedBytes.Count.ToString() + "/" + DBSize;
        }

        #endregion

        #region Add/Del bytes

        private void AddUsedByte(int StartByte, int ByteLength)
        {
            string log = "";
            for (int i = StartByte; i <= (StartByte + (ByteLength - 1)); i++)
            {
                if (!DBUsedBytes.Contains(i))
                {
                    DBUsedBytes.Add(i);
                    log += i + " | ";
                }
            }

            if (!string.IsNullOrEmpty(log))
            {
            //    ConsoleLog("Using bytes: " + log);
                Logger.Log("Using bytes: " + log);
            }

            DBUsedBytes.Sort();
            SetUsedBytes();
        }

        private void DelUsedByte(int StartByte, int ByteLength)
        {
            string log = "";
            for (int i = StartByte; i <= (StartByte + (ByteLength - 1)); i++)
            {
                if (DBUsedBytes.Contains(i))
                {
                    DBUsedBytes.Remove(i);
                    log += i + " | ";
                }
            }

            if (!string.IsNullOrEmpty(log))
            {
             //   ConsoleLog("Removing bytes: " + log);
                Logger.Log("Removing bytes: " + log);
            }

            DBUsedBytes.Sort();
            SetUsedBytes();
        }

        #endregion

        #region Window events

        // DB show/hide
        private void DbBar_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            GridData.Visibility = GridData.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
            lblCollapse.Content = GridData.Visibility == Visibility.Collapsed ? "4" : "6";

            lbl_ReadAll.Visibility = GridData.Visibility == Visibility.Collapsed ? Visibility.Hidden : Visibility.Visible;
            lbl_WriteAll.Visibility = GridData.Visibility == Visibility.Collapsed ? Visibility.Hidden : Visibility.Visible;

            lbl_ReadAll.IsEnabled = GridData.Visibility == Visibility.Collapsed ? false : true;
            lbl_WriteAll.IsEnabled = GridData.Visibility == Visibility.Collapsed ? false : true;
        }

        // DB context event
        private void DbBar_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (DBRightClicked != null)
                DBRightClicked(this,null);
        }

        // Address key
        private void Address_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                UnFocus(sender);
            }
        }

        private void Address_LostFocus(object sender, RoutedEventArgs e)
        {
            UnFocus(sender);
        }

        private void Address_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            TextBox address = (TextBox)sender;
            if (address.Tag != null)
            {
                address.Style = Resources["AddressEdit"] as Style;
                address.IsReadOnly = false;
            }
        }

        private void UnFocus(object sender)
        {
            TextBox address = (TextBox)sender;
            if (!address.IsReadOnly)
            {

                // Check curently entered address value
                int newvalue;
                bool IsParsed = Int32.TryParse(address.Text, out newvalue);

                if (IsParsed & (newvalue >= 0 && newvalue < DBSize))
                {
                    address.IsReadOnly = true;
                    address.Style = Resources["Address"] as Style;

                    //address.Tag = address.Text;
                    ComboBox comb = GetComboBox("cmbtype_" + address.Name.Split('_')[1]);
                    this.cmbtype_SelectionChanged(comb, null);
                }
                else
                {
                  //  ConsoleLog("Error: Entered value is not correct");

                    if (!IsParsed)
                        Logger.Log(address.Name + " value: " + address.Text + " is not a number");
                    else
                        Logger.Log(address.Name + " value: " + address.Text + " is out of range");
                }
            }
        }

        #endregion

        #region Enable/Disable

        private void DisableCombos()
        {
            foreach (ComboBox child in GridData.Children.OfType<ComboBox>())
            {
                child.IsEnabled = false;
            }
        }

        private void EnableCombos()
        {
            foreach (ComboBox child in GridData.Children.OfType<ComboBox>())
            {
                child.IsEnabled = true;
            }
        }

        private void DisableActValues()
        {
            foreach (TextBox child in GridData.Children.OfType<TextBox>())
            {
                if (child.Name.StartsWith("txtvalue_"))
                {
                    child.IsEnabled = false;
                }

            }
        }

        private void EnableActValues()
        {
            foreach (TextBox child in GridData.Children.OfType<TextBox>())
            {
                if (child.Name.StartsWith("txtvalue_"))
                {
                    child.IsEnabled = true;
                }

            }
        }

        private void DisableAddresses()
        {
            foreach (TextBox child in GridData.Children.OfType<TextBox>())
            {
                if (child.Name.StartsWith("blcaddress_"))
                {
                    child.IsEnabled = false;
                }

            }
        }

        private void EnableAddresses()
        {
            foreach (TextBox child in GridData.Children.OfType<TextBox>())
            {
                if (child.Name.StartsWith("blcaddress_"))
                {
                    child.IsEnabled = true;
                }

            }
        }

        #endregion

        #region Get GUI objects

        private ComboBox GetComboBox(string name)
        {
            ComboBox ActComboBox = null;

            foreach (ComboBox child in GridData.Children.OfType<ComboBox>())
            {

                if (child.Name.ToString() == name)
                {
                    ActComboBox = child;
                    break;
                }

            }

            if (ActComboBox == null)
            {
                Logger.Log("Could not find " + name, Logger.LogState.Warning);
            }

            return ActComboBox;
        }

        private Grid GetGrid(string name)
        {
            Grid ActBitBox = null;

            foreach (Grid child in GridData.Children.OfType<Grid>())
            {

                if (child.Name.ToString() == name)
                {
                    ActBitBox = child;
                    break;
                }

            }

            if (ActBitBox == null)
            {
                Logger.Log("Could not find " + name, Logger.LogState.Warning);
            }

            return ActBitBox;
        }

        private TextBox GetTextBox(string name)
        {
            TextBox ActAddresBox = null;

            foreach (TextBox child in GridData.Children.OfType<TextBox>())
            {

                if (child.Name.ToString() == name)
                {
                    ActAddresBox = child;
                    break;
                }

            }

            if (ActAddresBox == null)
            {
                Logger.Log("Could not find " + "blcaddress_" + name, Logger.LogState.Warning);
            }

            return ActAddresBox;
        }

        #endregion

        #region Adrress, data type, value manipulation

        private int GetLastFreeByte(int NeedLength = 1, bool FromMax = true, int StartFrom = 0)
        {

            if (DBUsedBytes.Count == 0)
            {
                return 0;
            }
            else
            {

                // Start byte
                int Start = 0;

                if (FromMax)
                {
                    Start = DBUsedBytes.Max() + 1;  // First avaiable byte
                }
                else
                {
                    // Test every byte from start value
                    bool TestNext = false;

                    for (int ActByte = StartFrom; ActByte < DBSize; ActByte++)
                    {
                        // Test space after every byte
                        for (int TestByte = ActByte; TestByte < (ActByte + NeedLength); TestByte++)
                        {
                            if (DBUsedBytes.Contains(TestByte))
                            {
                                TestNext = true; // There is not enough space  
                                break;           // Try another byte in array
                            }
                            TestNext = false;
                        }
                        // Test finished = TextNext = false
                        if (!TestNext)
                        {
                            Start = ActByte;
                            break;
                        }
                    }
                }

                if ((Start + NeedLength) > (DBSize))
                {
                    return -1; // Out of db memory
                }

                return Start;
            }
        }

        private void AddRow()
        {
            GridData.RowDefinitions.Add(new RowDefinition());
            int lastdatarow = GridData.RowDefinitions.Count - 1;

            TextBox address = new TextBox();
            ComboBox combo = new ComboBox();
            TextBox value = new TextBox();
            TextBox inputvalue = new TextBox();

            foreach (string type in combotypes)
            {
                combo.Items.Add(type);
            }

            // Address value
            address.Name = "blcaddress_" + lastdatarow;
            address.Style = Resources["Address"] as Style;
            address.IsReadOnly = true;
            address.Cursor = Cursors.Arrow;

            address.MouseDoubleClick += Address_MouseDoubleClick;
            address.LostFocus += Address_LostFocus;
            address.KeyDown += Address_KeyDown;

            // Data types
            combo.Name = "cmbtype_" + lastdatarow;
            combo.Style = Resources["DataType"] as Style;
            combo.SelectionChanged += cmbtype_SelectionChanged;

            // DB value
            value.Name = "txtvalue_" + lastdatarow;
            value.Style = Resources["ValueBox"] as Style;
            value.IsReadOnly = true;

            // Input value
            inputvalue.Name = "inputvalue_" + lastdatarow;
            inputvalue.Style = Resources["InputBox"] as Style;

            // Create new data row
            GridData.Children.Add(address);
            GridData.Children.Add(combo);
            GridData.Children.Add(value);
            GridData.Children.Add(inputvalue);

            Grid.SetRow(address, lastdatarow);
            Grid.SetRow(combo, lastdatarow);
            Grid.SetRow(value, lastdatarow);
            Grid.SetRow(inputvalue, lastdatarow);

            Grid.SetColumn(address, 0);
            Grid.SetColumn(combo, 1);
            Grid.SetColumn(value, 2);
            Grid.SetColumn(inputvalue, 3);

            int z = lastdatarow * (-1);
            Grid.SetZIndex(combo, z);

            

            //ScrollData.ScrollToBottom();

            Logger.Log("Add new line #" + lastdatarow);
        }

        // Data type changes
        private void cmbtype_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox actcombo = (ComboBox)sender;

            int selectedrow = Int32.Parse(actcombo.Name.Substring(actcombo.Name.IndexOf('_') + 1));
            int lastdatarow = GridData.RowDefinitions.Count - 1;

            TextBox ActAddresBox = GetTextBox("blcaddress_" + selectedrow.ToString());

            if (ActAddresBox == null)
            {
                return;
            }

            int needspace = 0;
            switch (actcombo.SelectedValue)
            {
                case "BIT":
                case "BYTE":
                case "CHAR":
                    needspace = 1;
                    break;
                case "INT":
                case "WORD":
                    needspace = 2;
                    break;
                case "DINT":
                case "DWORD":
                case "REAL":
                    needspace = 4;
                    break;
            }

            string log = "";
            int start = 0;

            // Test new/edit
            if (actcombo.Tag is null)
            {
                start = GetLastFreeByte(needspace);
                UsedIndexes.Add(selectedrow);
                log += "Action: new line";
            }
            else
            {
                // From what byte will delete
                int delstart = Int32.Parse(ActAddresBox.Tag.ToString());

                // How many bytes will be deleted
                int dellen = Int32.Parse(actcombo.Tag.ToString());

                // Delete unused bytes
                DelUsedByte(delstart, dellen);

                // Address text != Adrress tag => address changed
                if (ActAddresBox.Text != ActAddresBox.Tag.ToString())
                {
                    // From what byte I have free adrress?
                    start = GetLastFreeByte(needspace, false, Int32.Parse(ActAddresBox.Text));
                    log += "Action: edit address";
                }
                else
                {
                    // Data type change will continute from max free byte
                    start = GetLastFreeByte(needspace, false);
                    log += "Action: edit data type";
                }

            }
            log += " Type: " + actcombo.SelectedValue + " Bytes: " + needspace + " ";
            Logger.Log(log, Logger.LogState.Normal);

            AddUsedByte(start, needspace);          // Add newly used bytes

            // Set adrres box
            ActAddresBox.Text = start.ToString();
            ActAddresBox.Tag = ActAddresBox.Text;
            ActAddresBox.ToolTip = ActAddresBox.Tag.ToString();

            // Set data type combo
            actcombo.Tag = needspace;
            actcombo.ToolTip = actcombo.Tag.ToString();

            // Set value box 
            ChangeValueBox(selectedrow, actcombo.SelectedValue.ToString());

            if (selectedrow == lastdatarow)
            {
                AddRow();
            }
        }

        // Set valuebox BIT/Text
        private void ChangeValueBox(int selectedrow, string type)
        {
            // Check if textbox value exists
            TextBox valuebox = GetTextBox("txtvalue_" + selectedrow.ToString());
            bool IsTextBox = valuebox != null ? true : false;

            // Get input textbox
            TextBox InputValueBox = GetTextBox("inputvalue_" + selectedrow.ToString());

            // Check if grid with bit values exists
            Grid bitbox = GetGrid("bitgrid_" + selectedrow.ToString());
            Grid bitbox2 = GetGrid("bitgrid2_" + selectedrow.ToString());

            bool IsBitValue = bitbox != null ? true : false;

            // Add Textbox or grid 
            if (type == "BIT")
            {
                if (IsTextBox | !IsBitValue)
                {
                    //Remove text box
                    if (valuebox != null)
                    {
                        GridData.Children.RemoveAt(GridData.Children.IndexOf(valuebox));
                        GridData.Children.RemoveAt(GridData.Children.IndexOf(InputValueBox));
                    }

                    // Add bits
                    Grid GridBit = new Grid();      // 0 1 2 3 
                    Grid GridBit2 = new Grid();     // 4 5 6 7 
                    GridBit.RowDefinitions.Add(new RowDefinition());
                    GridBit2.RowDefinitions.Add(new RowDefinition());

                    for (int b = 0; b < 8; b++)
                    {
                        if (b < 4)
                        {
                            GridBit.ColumnDefinitions.Add(new ColumnDefinition());
                        }
                        else
                        {
                            GridBit2.ColumnDefinitions.Add(new ColumnDefinition());
                        }

                    }

                    GridBit.Name = "bitgrid_" + selectedrow;
                    GridBit.Style = Resources["bitgrid"] as Style;

                    GridBit2.Name = "bitgrid2_" + selectedrow;
                    GridBit2.Style = Resources["bitgrid"] as Style;

                    for (int b = 0; b < 8; b++)
                    {

                        Label bit = new Label();
                        bit.Content = b.ToString();
                        bit.Style = Resources["bitlabel"] as Style;
                        bit.Name = "bitvalue_" + selectedrow + "_" + b.ToString();
                        bit.Tag = b.ToString();                                         // Every label contains bit in TAG
                        bit.ToolTip = bit.Tag.ToString();

                        if (b < 4)
                        {
                            GridBit.Children.Add(bit);
                            Grid.SetRow(bit, 0);
                            Grid.SetColumn(bit, b);
                        }
                        else
                        {
                            GridBit2.Children.Add(bit);
                            Grid.SetRow(bit, 0);
                            Grid.SetColumn(bit, b - 4);
                        }

                    }

                    GridData.Children.Add(GridBit);
                    Grid.SetRow(GridBit, selectedrow);
                    Grid.SetColumn(GridBit, 2);

                    GridData.Children.Add(GridBit2);
                    Grid.SetRow(GridBit2, selectedrow);
                    Grid.SetColumn(GridBit2, 3);
                }
            }
            else
            {
                if (IsBitValue | !IsTextBox)
                {
                    // RemoveBit
                    if (bitbox != null)
                    {
                        GridData.Children.Remove(bitbox);
                        GridData.Children.Remove(bitbox2);
                    }
                    // Add textbox
                    TextBox newbox = new TextBox();
                    newbox.Name = "txtvalue_" + selectedrow;
                    newbox.Style = Resources["ValueBox"] as Style;

                    // Add textbox
                    TextBox newinput = new TextBox();
                    newinput.Name = "inputvalue_" + selectedrow;
                    newinput.Style = Resources["InputBox"] as Style;

                    SetDefaultValue(newbox, type);
                    SetDefaultValue(newinput, type);

                    GridData.Children.Add(newbox);
                    Grid.SetRow(newbox, selectedrow);
                    Grid.SetColumn(newbox, 2);

                    GridData.Children.Add(newinput);
                    Grid.SetRow(newinput, selectedrow);
                    Grid.SetColumn(newinput, 3);
                }
                else if (IsTextBox)
                {
                    SetDefaultValue(valuebox, type);
                    SetDefaultValue(InputValueBox, type);
                }
            }

        }

        private void SetDefaultValue(TextBox valuebox, string type)
        {
            valuebox.Text = type != "CHAR" ? "0" : "";
        }


        #endregion

        private void lbl_ReadAll_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {

        }

        private void lbl_WriteAll_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            WriteAll();
        }
    }
}

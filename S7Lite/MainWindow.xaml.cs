using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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
using System.Reflection;
using Snap7;
using System.Runtime.CompilerServices;

namespace S7Lite
{
    public partial class MainWindow : Window
    {
        S7Server server;
        Thread tserver;
        Thread watch;
        Thread read;

        int DB1Size = 1024;
        List<int> DB1UsedBytes = new List<int>();
        byte[] DB1;

        List<string> combotypes = new List<string> {"BIT","BYTE", "CHAR","WORD", "INT", "DWORD", "DINT", "REAL"};

        private Boolean WatchEnabled;
        private Boolean EnableWatch
        { 
            set
            {
                WatchEnabled = value;

                if (value)
                    StartWatchThread();
                else
                    StopWatchThread();
            }
            
            get
            {
                return WatchEnabled;
            }
        }

        private Boolean ReadingEnabled;
        private Boolean EnableReading
        {
            set
            {
                ReadingEnabled = value;
                if (value)
                    StartReadingThread();
                else
                    StopReadingThread();
            }
            get { return ReadingEnabled; }
        }
        
        // Thread server
        private Boolean _run;
        public Boolean run
        {
            get { return _run; }
            set { _run = value;}
        }

        public MainWindow()
        {
            InitializeComponent();
            Logger.Log("[ -- APP START -- ]");

            DB1 = new byte[DB1Size];

            SetGui();

            // Thread for watching other threads
            EnableWatch = true;
        }

        #region WatchThread

        private void StartWatchThread()
        {
            try { 
                if (watch != null)
                {
                    Logger.Log("Watch thread already exists");

                    if (!watch.IsAlive)
                    {
                        Logger.Log("Starting watch thread");
                        watch.Start();
                    }

                } else
                {
                    Logger.Log("Creating new watch thread");
                    watch = new Thread(() => { WatchThread(); });
                    Logger.Log("Starting watch thread");
                    watch.Start();
                }

                Logger.Log("Watch thread started");

            } catch (Exception ex)
            {
                ConsoleLog(ex.Message);
            }
        }

        private void StopWatchThread()
        {
            if (watch != null)
            {
                if (watch.IsAlive)
                {
                    Logger.Log("Trying to stop watch thread");
                    
                    Task killwatch = new Task(() =>
                    {
                       watch.Join();
                       return;
                    });  
                }
            }

            Logger.Log("Watch thread is dead");
        }

        private void WatchThread()
        {
            try
            {
                while (WatchEnabled)
                {
                    // Server status
                    lbl_Server.Dispatcher.Invoke(() => 
                    {
                        if (tserver != null)
                        {
                            lbl_Server.Style = tserver.IsAlive ? Resources["TopButtonOK"] as Style : Resources["TopButtonNOK"] as Style;
                        } else
                        {
                            lbl_Server.Style = Resources["TopButton"] as Style;
                        }
                    });

                    // Reading status
                    lbl_Read.Dispatcher.Invoke(() =>
                    {
                        if (read != null)
                        {
                            lbl_Read.Style = read.IsAlive ? Resources["TopButtonOK"] as Style : Resources["TopButtonNOK"] as Style;
                        }
                        else
                        {
                            lbl_Read.Style = Resources["TopButton"] as Style;
                        }
                    });


                    // CPU Status
                    string CPUStatus = "CPU Status";

                    Boolean IsOK = false;

                    if (server != null)
                    {
                        switch (server.CpuStatus)
                        {
                            case 0:
                                CPUStatus = "Unknown";
                                break;
                            case 8:
                                CPUStatus = "Run";
                                IsOK = true;
                                break;
                            case 4:
                                CPUStatus = "Stop";
                                break;
                        }


                        lbl_Online.Dispatcher.Invoke(() =>
                        {
                            lbl_Online.Style = IsOK ? Resources["TopButtonOK"] as Style : Resources["TopButtonNOK"] as Style;
                            lbl_Online.Content = CPUStatus;
                        });

                    }
                }
            } 
            catch (Exception ex)
            {

            } 
        }

        #endregion

        #region ReadThread

        private void StartReadingThread()
        {
            try
            {
                if (read != null)
                {
                    Logger.Log("Reading thread already exists");

                    if (!read.IsAlive)
                    {
                        Logger.Log("Starting reading thread");
                        read.Start();
                    }                   
                else
                {
                        Logger.Log("Creating new reading thread");
                        read = new Thread(() => { WatchThread(); });
                        Logger.Log("Starting reading thread");
                        read.Start();
                }

                    Logger.Log("Reading thread started");
                }
            } 
            catch (Exception ex)
            {
                
            }         
        }

        private void StopReadingThread()
        {
            if (read != null)
            {
                if (read.IsAlive)
                {
                    Logger.Log("Trying to stop reading thread");

                    Task killwatch = new Task(() =>
                    {
                        read.Join();
                        return;
                    });
                }
            }

            Logger.Log("Reading thread is dead");
        }

        private void ReadingThread()
        {
            while (EnableReading)
            {
                int rows = GridData.RowDefinitions.Count;

                for (int i = 0; i <= rows; i++)
                {

                }

            }
        }

        #endregion

        private Boolean StartServer()
        {
            server = new S7Server();
            server.RegisterArea(S7Server.S7AreaDB, 1, ref DB1, DB1.Length);
            return server.StartTo(cmb_ip.SelectedItem.ToString()) == 0 ? true : false;
        }

        private void StopServer()
        {
            try
            {
                if (server != null)
                {
                    server.CpuStatus = 4;
                    server.Stop();
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex.Message, Logger.LogState.Error);
            }
        }

        private void btn_connect_Click(object sender, RoutedEventArgs e)
        {
            try {

                if (!run)
                {
                    if (StartServer())
                    {
                        btn_connect.Content = "Stop";
                        
                        DisableCombos();
                        DisableAddresses();
                        DisableActValues();
             
                        tserver = new Thread(() => { ServerWork(); });
                        tserver.Name = "S7Server";
                        tserver.Start();

                        run = true;
                        ConsoleLog("Server started at " + cmb_ip.Text);
                    }
                } else
                {
                    run = false;
                    StopServer();
                    ConsoleLog("Stopping server");
                    btn_connect.Content = "Start";
                    EnableCombos();
                    EnableAddresses();
                    EnableActValues();
                }

            } catch (Exception ex)
            {
                Logger.Log("[" + MethodBase.GetCurrentMethod().Name + "]" + ex.Message, Logger.LogState.Error);
            }
        }

        private void ServerWork()
        {
            try
            {
                while (run)
                {
                    Thread.Sleep(100);
                }
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => { ConsoleLog(ex.Message); });
                Logger.Log(ex.Message, Logger.LogState.Error);
            }
            finally
            {
                Dispatcher.Invoke(() => { ConsoleLog("Server stopped"); });
            }
            
        }

        public void ConsoleLog(string msg)
        {
            if (!Dispatcher.CheckAccess())
            {
                LogConsole.Dispatcher.Invoke( () => { 
                    LogConsole.Text += DateTime.Now.ToString("T[HH:mm:ss] ") + msg + Environment.NewLine;
                    Scroll.ScrollToBottom();
                });
            } 
            else
            {
                LogConsole.Text += DateTime.Now.ToString("[HH:mm:ss] ") + msg + Environment.NewLine;
                Scroll.ScrollToBottom();
            }
        }

        private int GetLastFreeByte(int NeedLength = 1, bool FromMax = true, int StartFrom = 0)
        {

            if (DB1UsedBytes.Count == 0)
            {
                return 0;
            }
            else {

                // Start byte
                int Start = 0;

                if (FromMax)
                {
                    Start = DB1UsedBytes.Max() + 1;  // First avaiable byte
                }
                else
                {
                   // Test every byte from start value
                    bool TestNext = false; 

                    for (int ActByte = StartFrom; ActByte < DB1Size;ActByte++)
                    {
                        // Test space after every byte
                        for (int TestByte = ActByte; TestByte < (ActByte + NeedLength); TestByte++)
                        {
                            if (DB1UsedBytes.Contains(TestByte))
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

                if ((Start + NeedLength) > (DB1Size))
                {
                    return -1; // Out of db memory
                }

                return Start;
            }
        }

        #region Utils

        private void SetGui()
        {
            AddRow();
            GetIp();
            lblUsedBytes.Text = "0";
            lbl_Version.Content = "v0.0";
        }

        private void GetIp()
        {
            cmb_ip.Items.Clear();
            IPAddress[] localIPs = Dns.GetHostAddresses(Dns.GetHostName());
            foreach (IPAddress ip in localIPs)
            {
                if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    cmb_ip.Items.Add(ip.ToString());
                }
            }
            if (cmb_ip.HasItems) cmb_ip.SelectedIndex = 0;
        }

        private void SetUsedBytes()
        {
            lblUsedBytes.Text = DB1UsedBytes.Count.ToString() + "/" + DB1Size;
        }

        #endregion

        #region Add/Del bytes

        private void AddUsedByte(int StartByte, int ByteLength)
        {
            string log = "";
            for (int i = StartByte; i <= (StartByte + (ByteLength-1)); i++)
            {
                if (!DB1UsedBytes.Contains(i))
                {
                    DB1UsedBytes.Add(i);
                    log += i + " | ";
                }
            }

            if (!string.IsNullOrEmpty(log))
            {
                ConsoleLog("Using bytes: " + log);
                Logger.Log("Using bytes: " + log);
            }

            DB1UsedBytes.Sort();
            SetUsedBytes();
        }

        private void DelUsedByte(int StartByte, int ByteLength)
        {
            string log = "";
            for (int i = StartByte; i <= (StartByte + (ByteLength - 1)); i++)
            {
                if (DB1UsedBytes.Contains(i))
                {
                    DB1UsedBytes.Remove(i);
                    log += i + " | ";
                }
            }

            if (!string.IsNullOrEmpty(log))
            {
                ConsoleLog("Removing bytes: " + log);
                Logger.Log("Removing bytes: " + log);
            }

            DB1UsedBytes.Sort();
            SetUsedBytes();
        }

        #endregion

        #region Window events

        private void Label_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (RowLog.Height.Value == 0)
            {
                RowLog.Height = new GridLength(4, GridUnitType.Star);
            }
            else
            {
                RowLog.Height = new GridLength(0);
            }
        }

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

                if (IsParsed & (newvalue >= 0 && newvalue < DB1Size))
                {
                    address.IsReadOnly = true;
                    address.Style = Resources["Address"] as Style;

                    //address.Tag = address.Text;
                    ComboBox comb = GetComboBox("cmbtype_" + address.Name.Split('_')[1]);
                    this.cmbtype_SelectionChanged(comb, null);
                } 
                else
                {
                    ConsoleLog("Error: Entered value is not correct");

                    if (!IsParsed)
                        Logger.Log(address.Name + " value: " + address.Text + " is not a number");
                    else
                        Logger.Log(address.Name + " value: " + address.Text + " is out of range");
                }
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (tserver != null)
            {
                run = false;

                if (tserver.IsAlive)
                {
                    tserver.Join(1000);
                }
            }

            EnableWatch = false;

            Logger.Log("[ -- APP CLOSE -- ]");
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

            ScrollData.ScrollToBottom();

            Logger.Log("Add new line #" + lastdatarow);
        }

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
                log += "Action: new line";
            } else
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
                } else
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
                    if (valuebox != null) { 
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
                    if (bitbox != null) { 
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

        private void SetDefaultValue(TextBox valuebox,string type)
        {
            valuebox.Text = type != "CHAR" ? "0" : "";
        }

        #endregion
    }
}

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

        int DB1Size = 1024;
        List<int> DB1UsedBytes = new List<int>();
        byte[] DB1;

        List<string> combotypes = new List<string> {"BIT","BYTE", "CHAR","WORD", "INT", "DWORD", "DINT", "REAL"};

        // Thread server
        private Boolean _run;
        public Boolean run
        {
            get { return _run; }
            set
            {
                _run = value;;
            }
        }

        public MainWindow()
        {
            InitializeComponent();

            Logger.Log("[ -- APP START -- ]");

            DB1 = new byte[DB1Size];

            SetGui();
        }

        private void SetGui()
        {
            AddRow();
            GetIp();
            lblUsedBytes.Text = "0";
            lbl_Version.Content = "v0.0";
        }

        private void SetUsedBytes()
        {
            lblUsedBytes.Text = DB1UsedBytes.Count.ToString() + "/" + DB1Size;
        }

        private void GetIp()
        {
            cmb_ip.Items.Clear();
            IPAddress[] localIPs = Dns.GetHostAddresses(Dns.GetHostName());
            foreach (IPAddress ip in localIPs)
            {
                if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    cmb_ip.Items.Add(ip.ToString()) ;
                }
            }
            if (cmb_ip.HasItems) cmb_ip.SelectedIndex = 0;
        }

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

                CheckFinalDb();

                if (!run)
                {
                    if (StartServer())
                    {
                        btn_connect.Content = "Stop";
                        
                        DisableCombos();
             
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
                }

            } catch (Exception ex)
            {
                Logger.Log("[" + MethodBase.GetCurrentMethod().Name + "]" + ex.Message, Logger.LogState.Error);
            }
        }

        private void CheckFinalDb()
        {

        }

        private void ServerWork()
        {
            try
            {
                while (run)
                {
                    TestServer();
                }
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => { ConsoleLog(ex.Message); });
                Logger.Log(ex.Message, Logger.LogState.Error);
            }
            finally
            {
                //btn_connect.Dispatcher.Invoke(() => { btn_connect.Content = "Stop"; });

                Dispatcher.Invoke(() => { ConsoleLog("Server stopped"); });
                //Logger.Log("[" + MethodInfo.GetCurrentMethod().Name + "]" + " Server stopped");
            }
            
        }

        private void TestServer()
        {
            Thread.Sleep(1000);
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

            Logger.Log("[ -- APP CLOSE -- ]");
        }

        public void ConsoleLog(string msg)
        {
            LogConsole.Text += DateTime.Now.ToString("[HH:mm:ss] ") + msg + Environment.NewLine;
            Scroll.ScrollToBottom();
        }

        /// <summary>
        /// Tests if there is required space in DB and returns start byte
        /// </summary>
        /// <param name="NeedLength">How many bytes is needed</param>
        /// <returns></returns>
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

        #region Click events

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
                    ComboBox comb = GetComboBox(address.Name.Split('_')[1]);
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

        #endregion

        #region Enable/Disable combos

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

        #endregion

        #region Get GUI objects
        
        private ComboBox GetComboBox(string name)
        {
            ComboBox ActComboBox = null;

            foreach (ComboBox child in GridData.Children.OfType<ComboBox>())
            {

                if (child.Name.ToString() == "cmbtype_" + name)
                {
                    ActComboBox = child;
                    break;
                }

            }

            if (ActComboBox == null)
            {
                Logger.Log("Could not find " + "cmbtype_" + name, Logger.LogState.Warning);
            }

            return ActComboBox;
        }

        private Label GetBitValueBox(string name)
        {
            Label ActBitBox = null;

            foreach (Label child in GridData.Children.OfType<Label>())
            {

                if (child.Name.ToString() == "bitvalue_" + name)
                {
                    ActBitBox = child;
                    break;
                }

            }

            if (ActBitBox == null)
            {
                Logger.Log("Could not find " + "bitvalue_" + name, Logger.LogState.Warning);
            }

            return ActBitBox;
        }

        private TextBox GetValueTextBox(string name)
        {
            TextBox ActAddresBox = null;

            foreach (TextBox child in GridData.Children.OfType<TextBox>())
            {

                if (child.Name.ToString() == "txtvalue_" + name)
                {
                    ActAddresBox = child;
                    break;
                }

            }

            if (ActAddresBox == null)
            {
                Logger.Log("Could not find " + "txtvalue_" + name, Logger.LogState.Warning);
            }

            return ActAddresBox;
        }

        private TextBox GetAddressTextBox(string name)
        {
            TextBox ActAddresBox = null;

            foreach (TextBox child in GridData.Children.OfType<TextBox>())
            {

                if (child.Name.ToString() == "blcaddress_" + name)
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

        private void AddRow()
        {
            GridData.RowDefinitions.Add(new RowDefinition());
            int lastdatarow = GridData.RowDefinitions.Count - 1;

            //TextBlock address = new TextBlock();
            TextBox address = new TextBox();
            TextBox value = new TextBox();
            ComboBox combo = new ComboBox();

            foreach (string type in combotypes)
            {
                combo.Items.Add(type);
            }

            // Data types
            combo.Name = "cmbtype_" + lastdatarow;
            combo.Style = Resources["DataType"] as Style;
            combo.SelectionChanged += cmbtype_SelectionChanged;

            // DB value
            value.Name = "txtvalue_" + lastdatarow;

            // Address value
            address.Name = "blcaddress_" + lastdatarow;
            address.Style = Resources["Address"] as Style;
            address.IsReadOnly = true;
            address.Cursor = Cursors.Arrow;

            address.MouseDoubleClick += Address_MouseDoubleClick;
            address.LostFocus += Address_LostFocus;
            address.KeyDown += Address_KeyDown;

            // Create new data row
            GridData.Children.Add(address);
            GridData.Children.Add(value);
            GridData.Children.Add(combo);

            Grid.SetRow(address, lastdatarow);
            Grid.SetRow(value, lastdatarow);
            Grid.SetRow(combo, lastdatarow);

            Grid.SetColumn(address, 0);
            Grid.SetColumn(combo, 1);
            Grid.SetColumn(value, 2);

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

            TextBox ActAddresBox = GetAddressTextBox(selectedrow.ToString());

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

            // New row vs edit row
            if (actcombo.Tag is null) 
            {
                start = GetLastFreeByte(needspace);
                log += "Action: new line";
            } else
            {
                int delstart = Int32.Parse(ActAddresBox.Tag.ToString());
                int dellen = Int32.Parse(actcombo.Tag.ToString());
                DelUsedByte(delstart, dellen);

                if (ActAddresBox.Text != ActAddresBox.Tag.ToString())
                {
                    start = GetLastFreeByte(needspace, false, Int32.Parse(ActAddresBox.Text));
                    log += "Action: edit address";
                } else
                {
                    start = GetLastFreeByte(needspace, false);
                    log += "Action: edit data type";
                }

            }
            log += " Type: " + actcombo.SelectedValue + " Bytes: " + needspace + " ";
            Logger.Log(log, Logger.LogState.Normal);

            AddUsedByte(start, needspace);
            ActAddresBox.Text = start.ToString();
            ActAddresBox.Tag = ActAddresBox.Text;
            actcombo.Tag = needspace;

            ChangeValueBox(selectedrow, actcombo.SelectedValue.ToString());

            if (selectedrow == lastdatarow)
            {
                AddRow();
            }
        }

        private void ChangeValueBox(int selectedrow, string type)
        {
            // Check if textbox value exists
            TextBox valuebox = GetValueTextBox(selectedrow.ToString());
            bool IsTextBox = valuebox != null ? true : false;

            // Check if bit value exists
            Label bitbox = GetBitValueBox(selectedrow.ToString());
            bool IsBitValue = bitbox != null ? true : false;

            if (type == "BIT")
            {
                if (IsTextBox | !IsBitValue)
                {
                    //Remove text box
                    GridData.Children.RemoveAt(GridData.Children.IndexOf(valuebox));

                    // Add bit panel
                    StackPanel stack = new StackPanel();
                    stack.Orientation = Orientation.Horizontal;

                    stack.Name = "stack_" + selectedrow;

                    for (int b = 0; b < 8; b++)
                    {
                        Label bit = new Label();
                        bit.Content = b.ToString();
                        bit.Name = "bitvalue_" + selectedrow + "_" + b.ToString();
                        stack.Children.Add(bit);
                    }

                    GridData.Children.Add(stack);
                    Grid.SetRow(stack, selectedrow);
                    Grid.SetColumn(stack, 2);
                }
            }
            else
            {
                if (IsBitValue | !IsTextBox)
                {
                    // RemoveBit


                    // Add textbox
                    TextBox newbox = new TextBox();
                    newbox.Name = "txtvalue_" + selectedrow;
                    GridData.Children.Add(newbox);
                    Grid.SetRow(newbox, selectedrow);
                    Grid.SetColumn(newbox, 2);

                }
            }

        }
    }
}

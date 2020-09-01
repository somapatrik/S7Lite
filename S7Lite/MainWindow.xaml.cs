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

                if (!run)
                {
                    if (StartServer())
                    {
                        btn_connect.Content = "Stop";
                        ConsoleLog("Server started at " + cmb_ip.Text);
                        DisableCombos();
                        run = true;

                        tserver = new Thread(() => { ServerWork(); });
                        tserver.Name = "S7Server";
                        tserver.Start();
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
                Logger.Log("[" + MethodBase.GetCurrentMethod().Name + "]" + ex.Message);
            }
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
            //Logger.Log(msg, Logger.LogState.Normal);
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
                address.IsReadOnly = true;
                address.Style = Resources["Address"] as Style;

                //address.Tag = address.Text;
                ComboBox comb = GetComboBox(address.Name.Split('_')[1]);
                this.cmbtype_SelectionChanged(comb, null);

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
                ConsoleLog("Could not find " + "cmbtype_" + name);
            }

            return ActComboBox;
        }

        private TextBox GetTextBox(string name)
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
                ConsoleLog("Could not find " + "blcaddress_" + name);
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
        }

        private void cmbtype_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

            ComboBox actcombo = (ComboBox)sender;

            int selectedrow = Int32.Parse(actcombo.Name.Substring(actcombo.Name.IndexOf('_') + 1));
            int lastdatarow = GridData.RowDefinitions.Count - 1;

            TextBox ActAddresBox = GetTextBox(selectedrow.ToString());

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

            StringBuilder log = new StringBuilder();
            log.Append("Type: " + actcombo.SelectedValue + " ");
            log.Append("Bytes: " + needspace + " ");


            int start = 0;

            // New row vs edit row
            if (actcombo.Tag is null) 
            {
                start = GetLastFreeByte(needspace);
            } else
            {
                int delstart = Int32.Parse(ActAddresBox.Tag.ToString());
                DelUsedByte(delstart, Int32.Parse(actcombo.Tag.ToString()));

                if (ActAddresBox.Text != ActAddresBox.Tag.ToString())
                {
                    start = GetLastFreeByte(needspace, false, Int32.Parse(ActAddresBox.Text));
                } else
                {
                    start = GetLastFreeByte(needspace, false);
                }

            }

            AddUsedByte(start, needspace);
            ActAddresBox.Text = start.ToString();
            ActAddresBox.Tag = ActAddresBox.Text;
            actcombo.Tag = needspace;
            
            if (selectedrow == lastdatarow)
            {
                AddRow();
            }

        }
    }
}

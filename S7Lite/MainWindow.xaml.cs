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

        List<string> combotypes = new List<string> {"BIT","BYTE","WORD", "INT", "DWORD", "DINT", "REAL", "CHAR"};

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
            cmb_ip.Items.Clear();
            GetIp();
        }

        private void GetIp()
        {
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
                        ConsoleLog("Server started");
                        Logger.Log("Server started at " + cmb_ip.Text);
                        GridData.IsEnabled = false;
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
                    Logger.Log("Stopping server");
                    btn_connect.Content = "Start";
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
                btn_connect.Dispatcher.Invoke(() => { btn_connect.Content = "Stop"; });

                Dispatcher.Invoke(() => { ConsoleLog("Server stopped"); });
                Logger.Log("[" + MethodInfo.GetCurrentMethod().Name + "]" + " Server stopped");
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
        /// Hide/Show console log
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Label_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (RowLog.Height.Value == 0)
            {
                RowLog.Height = new GridLength(4, GridUnitType.Star);
            } else
            {
                RowLog.Height = new GridLength(0);
            }
        }

        /// <summary>
        /// Tests if there is required space in DB and returns start byte
        /// </summary>
        /// <param name="NeedLength">How many bytes is needed</param>
        /// <returns></returns>
        private int GetLastFreeByte(int NeedLength = 1, int StartByte = -10)
        {

            if (DB1UsedBytes.Count == 0)
            {
                return 0;
            }
            else {

                // Start byte
                int Start = 0;

                if (StartByte == -10)
                {
                    Start = DB1UsedBytes.Max() + 1;  // First avaiable byte
                } 
                else
                {
                    foreach(int Act in DB1UsedBytes)
                    {
                        int Next = Act + 1;
                        if (DB1UsedBytes.Contains(Next))
                        {
                            Start = Next;
                            break;
                        }
                    }
                }

                // Start byte located
                // Do I have required space left?
                Boolean Found = false;
                for (int i = Start; i <= (Start + (NeedLength - 1)); i++)
                {
                    if (DB1UsedBytes.Contains(i))
                    {
                        Found = true;
                        break;
                    }
                }

                if (Found)
                {
                    GetLastFreeByte(NeedLength, Start + 1);
                }

                return Start;
            }
        }

        private void AddUsedByte(int StartByte, int ByteLength)
        {
            if (!DB1UsedBytes.Contains(StartByte))
            {
                for (int i = StartByte; i <= (StartByte + (ByteLength-1)); i++)
                {
                    DB1UsedBytes.Add(i);
                }
            }
        }

        private void DelUsedByte(int StartByte, int ByteLength)
        {
            if (DB1UsedBytes.Contains(StartByte))
            {
                for (int i = StartByte; i <= (StartByte + (ByteLength - 1)); i++)
                {
                    DB1UsedBytes.Remove(i);
                }
            }
        }

        private void AddRow()
        {
            GridData.RowDefinitions.Add(new RowDefinition());
            int lastdatarow = GridData.RowDefinitions.Count - 1;

            TextBlock address = new TextBlock();
            TextBox value = new TextBox();
            ComboBox combo = new ComboBox();

            foreach(string type in combotypes)
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
            //address.Text = GetLastFreeByte().ToString();
            address.Style = Resources["Address"] as Style;

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

            TextBlock ActAddresBox = null;

            foreach (TextBlock child in GridData.Children.OfType<TextBlock>())
            {

                if (child.Name.ToString() == "blcaddress_" + selectedrow.ToString())
                {
                    ActAddresBox = child;
                    break;
                }

            }

            if (ActAddresBox == null)
            {
                ConsoleLog("Could not find "+ "blcaddress_" + selectedrow.ToString());
                return;
            }

            int needspace = 0;
            switch (actcombo.SelectedValue)
            {
                case "BIT":
                case "BYTE":
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
            
            int start = 0;

            // If not last row remove used bytes first
            if (selectedrow < lastdatarow)
            {
                int delstart = Int32.Parse(ActAddresBox.Text);
                DelUsedByte(delstart, (Int32)ActAddresBox.Tag);
                start = GetLastFreeByte(needspace, delstart);
            } else
            {
                start = GetLastFreeByte(needspace);
            }

            
            AddUsedByte(start, needspace);

            ActAddresBox.Text = start.ToString();
            ActAddresBox.Tag = needspace;
            
            if (selectedrow == lastdatarow)
            {
                AddRow();
            }

        }
    }
}

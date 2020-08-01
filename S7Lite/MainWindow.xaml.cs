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
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        S7Server server;
        Thread tserver;

        int DB1Size = 256;
        int DB1FreeByte = 0;
        byte[] DB1;

        List<string> combotypes = new List<string> {"BOOL", "INT", "DINT", "REAL", "CHAR"};

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

        private void Button_Click(object sender, RoutedEventArgs e)
        {
           AddRow();
        }

        private void AddRow()
        {

            GridData.RowDefinitions.Add(new RowDefinition());

            // Newly added row
            int lastrow = GridData.RowDefinitions.Count - 1;
            int lastdatarow = lastrow - 1;

            TextBlock address = new TextBlock();
            TextBox value = new TextBox();
            ComboBox combo = new ComboBox();

            foreach(string type in combotypes)
            {
                combo.Items.Add(type);
            }

            combo.SelectionChanged += cmbtype_SelectionChanged;
            combo.Name = "cmbtype_" + lastdatarow.ToString();

            address.Name = "blcaddress_" + lastdatarow.ToString();
            value.Name = "txtvalue_" + lastdatarow.ToString();

            //combo.Style = Resources["DataTypeCombo"] as Style;
            address.Style = Resources["Address"] as Style;

            // Get button to last row
            Grid.SetRow(btnAdd, lastrow);

            // New data row
            GridData.Children.Add(address);
            GridData.Children.Add(value);
            GridData.Children.Add(combo);

            Grid.SetRow(address, lastdatarow);
            Grid.SetRow(value, lastdatarow);
            Grid.SetRow(combo, lastdatarow);

            Grid.SetColumn(address, 1);
            Grid.SetColumn(combo, 0);
            Grid.SetColumn(value, 2);

            int z = lastdatarow * (-1);
            Grid.SetZIndex(combo, z);

            ScrollData.ScrollToBottom();
        }

        private void cmbtype_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox actcombo = (ComboBox)sender;

            int selectedrow = Int32.Parse(actcombo.Name.Substring(actcombo.Name.IndexOf('_') + 1));
            int lastdatarow = GridData.RowDefinitions.Count - 2;

            if (selectedrow == lastdatarow)
            {
                AddRow();
            }

            TextBlock ActAddresBox = null;

            foreach (TextBlock child in GridData.Children.OfType<TextBlock>()) {

                if (child.Name.ToString() == "blcaddress_" + selectedrow.ToString()) 
                {
                    ActAddresBox = child;
                    break;
                }

            }

             

            switch (actcombo.SelectedValue)
            {
                case "BOOL":
                    DB1FreeByte += 1;
                    break;
                case "INT":
                    actcombo.Tag = DB1FreeByte;
                    ActAddresBox.Text = "DB1." + "DBW" + actcombo.Tag.ToString() ;
                    DB1FreeByte += 1;
                    break;
                case "DINT":
                    DB1FreeByte += 4;
                    break;
                case "REAL":
                    DB1FreeByte += 8;
                    break;
                case "CHAR":
                    DB1FreeByte += 1;
                    break;
            }



        }
    }
}

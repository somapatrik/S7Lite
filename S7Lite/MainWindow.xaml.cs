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
using Snap7;

namespace S7Lite
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        S7Server server;
        byte[] DB1 = new byte[256];
        Thread tserver;
        
        private Boolean _run;
        public Boolean run
        {
            get { return _run; }
            set { _run = value;}
        }

        public MainWindow()
        {
            InitializeComponent();
            SetGui();
        }

        private void SetGui()
        {
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

        private void btn_connect_Click(object sender, RoutedEventArgs e)
        {
            try { 
                if (!run)
                {
                    if (StartServer())
                    {
                        Log("Server started");
                        run = true;
                        tserver = new Thread(() => { ServerWork(); });
                        tserver.Name = "S7Server";
                        tserver.Start();
                    }
                } else
                {
                    run = false;
                    Log("Stopping server");
            }
            } catch (Exception ex)
            {
                Log(ex.Message);
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
                Dispatcher.Invoke(new dlg_Log(Log), ex.Message);
            }
            finally
            {
                run = false;
                Dispatcher.Invoke(new dlg_Log(Log), "Server stopped");
            }
            
        }

        private void TestServer()
        {
            Thread.Sleep(1000);
            Dispatcher.Invoke(new dlg_Log(Log), "Run");
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
        }

        private delegate void dlg_Log(string msg);
        public void Log(string msg)
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
    }
}

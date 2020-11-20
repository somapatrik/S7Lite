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
using S7Lite.Class;

namespace S7Lite
{
    public partial class MainWindow : Window
    {
        // Max DBSize
        int DBMaxSize = 1024;

        // Memory with original DB values
        List<DB> DBMemory = new List<DB>();

        public MainWindow()
        {
            InitializeComponent();
            Logger.Log("[ -- APP START -- ]");

            PlcServer.IniServer();
            PlcServer.UpdatedDB += PlcServer_UpdatedDB;

            SetGUI();
        }

        private void PlcServer_UpdatedDB(object sender, EventArgs e)
        {
            try
            {
                int s = (int)sender;
                MessageBox.Show(s.ToString());
            } catch (Exception ex)
            {
                Logger.Log(ex.Message, Logger.LogState.Error);
            }
        }

     

        private void SetGUI()
        {
            // Events
            txtDBNumber.TextChanged += TxtDBNumber_TextChanged;

            // Load possible server IP
            GetIp();
        }

        #region GUI events

        // IP address changed
        private void cmb_ip_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            PlcServer.PLC_IP = cmb_ip.SelectedValue.ToString();
        }

        // Start button
        private void lbl_start_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!PlcServer.IsRunning)
            {
                PlcServer.StartPLCServer();
                lbl_start.Content = "STOP";
            }
            else
            {
                PlcServer.StopPLCServer();
                lbl_start.Content = "START";
            }
        }

        // DB number changed
        private void TxtDBNumber_TextChanged(object sender, TextChangedEventArgs e)
        {
            int val;
            if (!Int32.TryParse(txtDBNumber.Text, out val))
            {
                txtDBNumber.Text = PlcServer.GetAvailableDB().ToString();
            }
            
        }

        // Add DB button
        private void lblAddDB_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (PlcServer.IsDbAvailable(Int32.Parse(txtDBNumber.Text)))
                {
                    // Original object
                    DB newdb = new DB(Int32.Parse(txtDBNumber.Text), new byte[DBMaxSize]);
                    DBMemory.Add(newdb);

                    // PLC Server reference
                    PlcServer.AddDB(ref newdb);

                    // GUI Controls reference
                    DBControl dbcontrol = new DBControl(ref newdb);
                    dbcontrol.DBRightClicked += Dbcontrol_DBRightClicked;
                    DBStack.Children.Add(dbcontrol);

                    txtDBNumber.Text = PlcServer.GetAvailableDB().ToString();
                }
                else
                {
                    LogGUI("DB " + txtDBNumber.Text + " already exists");
                }
            } catch (Exception ex)
            {
                Logger.Log(ex.Message, Logger.LogState.Error);
            }

        }

        // Log label
        private void Label_MouseUp(object sender, MouseButtonEventArgs e)
        {
            RowLog.Height = RowLog.Height.Value == 0 ? new GridLength(1, GridUnitType.Star) : new GridLength(0);
        }

        #endregion

        #region Context menu

        // DB Control right click event
        private void Dbcontrol_DBRightClicked(object sender, EventArgs e)
        {
            PopupDB pop = new PopupDB(((DBControl)sender).DBNumber);
            PopUpGrid.Children.Add(pop);
            pop.FirstClicked += Pop_FirstClicked;
        }

        // Context menu first button event
        private void Pop_FirstClicked(object sender, EventArgs e)
        {
            PopupDB pop = (PopupDB)sender;
            RemoveDB(pop.DBNumber);
            PopUpGrid.Children.Remove(pop);
        }

        #endregion

        #region Utils

        private void RemoveDB(int num)
        {
            // Remove all ref from memory
            PlcServer.DBRemove(num);
            // Remove GUI control
            foreach (DBControl control in DBStack.Children)
            {
                if (control.DBNumber == num)
                {
                    DBStack.Children.Remove(control);
                    break;
                }            
            }
            // Clear memory
            DB old = DBMemory.Find(o => o.number == num);
            DBMemory.Remove(old);
            old = null;
        }

        private void GetIp()
        {
            cmb_ip.Items.Clear();
            IPAddress[] localIPs = Dns.GetHostAddresses(Dns.GetHostName());
            foreach (IPAddress ip in localIPs)
            {
                if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    cmb_ip.Items.Add(ip.ToString());
            }
            if (cmb_ip.HasItems) cmb_ip.SelectedIndex = 0;
        }

        private async void LogGUI(string msg)
        {
            await Task.Run(() => {
                string raw = msg;
                string line = "[" + DateTime.Now.ToString("HH:mm:ss") + "] " + raw + Environment.NewLine;

                LogConsole.Dispatcher.Invoke(() => {
                    LogConsole.Tag = LogConsole.Tag == null ? 0 : (int)LogConsole.Tag + 1;

                    if ((int)LogConsole.Tag >= 20)
                    {
                        LogConsole.Text = null;
                        LogConsole.Tag = 0;
                    }

                    LogConsole.Text += line;
                    Scroll.ScrollToEnd();
                });
            });
        }

        #endregion
    }
}

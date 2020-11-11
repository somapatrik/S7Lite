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

            SetGUI();
        }

        private void SetGUI()
        {
            // Events
            txtDBNumber.TextChanged += TxtDBNumber_TextChanged;

            // Load possible server IP
            GetIp();
        }

        private void TxtDBNumber_TextChanged(object sender, TextChangedEventArgs e)
        {
            int val;
            if (!Int32.TryParse(txtDBNumber.Text, out val))
            {
                txtDBNumber.Text = PlcServer.GetAvailableDB().ToString();
            }
            
        }

        private void lblAddDB_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
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

        }

        private void Dbcontrol_DBRightClicked(object sender, EventArgs e)
        {
            PopupDB pop = new PopupDB(((DBControl)sender).DBNumber);
            PopUpGrid.Children.Add(pop);
            pop.FirstClicked += Pop_FirstClicked;
        }

        private void Pop_FirstClicked(object sender, EventArgs e)
        {
            PopupDB pop = (PopupDB)sender;
            RemoveDB(pop.DBNumber);
            PopUpGrid.Children.Remove(pop);
        }

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

        private void Label_MouseUp(object sender, MouseButtonEventArgs e)
        {
            RowLog.Height = RowLog.Height.Value == 0 ? new GridLength(1, GridUnitType.Star) : new GridLength(0);
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

        private void cmb_ip_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            PlcServer.PLC_IP = cmb_ip.SelectedValue.ToString();
        }

        private void Label_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            foreach (DBControl db in DBStack.Children)
            {
                db.Activate();
            }             
        }

        


    }
}

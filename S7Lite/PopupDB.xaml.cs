using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
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

namespace S7Lite
{
    public partial class PopupDB : UserControl
    {
        public event EventHandler FirstClicked;
        public int DBNumber { get; set; }

        public PopupDB(int DBnum)
        {
            this.DBNumber = DBnum;
            InitializeComponent();
            CreateButtons();
        }

        private void CreateButtons()
        {
            Label first = new Label();
            Label second = new Label();

            first.Content = "Remove DB " + DBNumber.ToString();
            second.Content = "Close";

            first.MouseLeftButtonUp += First_MouseLeftButtonUp;
            second.MouseLeftButtonUp += Second_MouseLeftButtonUp;

            first.Style = Resources["PopButton"] as Style;
            second.Style = Resources["PopButton"] as Style;

            stackpopbutt.Children.Add(first);
            stackpopbutt.Children.Add(second);
        }

        private void Second_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            CloseMe();
        }

        private void CloseMe()
        {
            ((Grid)this.Parent).Children.Remove(this);
        }

        private void First_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (FirstClicked != null)
            {
                FirstClicked(this, null);
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace TradeControl.Bitcoin
{
    /// <summary>
    /// Interaction logic for NameWindow.xaml
    /// </summary>
    public partial class NameWindow : Window
    {
        public NameWindow()
        {
            InitializeComponent();
        }
        
        public string NamespaceKey { get { return textNamespace.Text; } set { textNamespace.Text = value; } }

        public string KeyName {  get { return textKeyName.Text; } set { textKeyName.Text = value; } }

        private void btnAccept_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            textKeyName.Focus();
        }
    }
}

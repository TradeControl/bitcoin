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
using TradeControl.Node;

namespace TradeControl.Bitcoin
{
    /// <summary>
    /// Interaction logic for AssignKeyWindow.xaml
    /// </summary>
    public partial class AssignKeyWindow : Window
    {
        public AssignKeyWindow()
        {
            InitializeComponent();
        }

        public string KeyNamespace
        {
            get { return textKeyNamespace.Text;  }
            private set { textKeyNamespace.Text = value; }
        }

        public string InvoiceNumber
        {
            get { return textInvoiceNumber.Text; } 
            set { textInvoiceNumber.Text = value; }
        }

        public string PaymentAddress
        {
            get { return textPaymentAddress.Text; }
            private set { textPaymentAddress.Text = value; }
        }

        public string Note
        {
            get { return textNote.Text; }
            private set { textNote.Text = value; }
        }

        public string KeyName
        {
            get; private set;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            dgReceiptKeys.Focus();
        }

        private void btnAccept_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = (dgReceiptKeys.SelectedItem != null);
        }

        private void dgReceiptKeys_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgReceiptKeys.SelectedItem != null)
            {
                var item = (fnChangeUnassignedResult)dgReceiptKeys.SelectedItem;
                KeyNamespace = item.KeyNamespace;
                Note = item.Note;
                PaymentAddress = item.PaymentAddress;
                KeyName = item.KeyName;
            }
        }
    }
}

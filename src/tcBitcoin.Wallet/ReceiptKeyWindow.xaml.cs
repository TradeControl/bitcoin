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
    /// Interaction logic for NewReceiptKeyWindow.xaml
    /// </summary>
    public partial class ReceiptKeyWindow : Window
    {
        public ReceiptKeyWindow(string paymentAddress) : this(paymentAddress, string.Empty) {}

        public ReceiptKeyWindow(string paymentAddress, string invoiceNumber) 
        {
            InitializeComponent();

            textPaymentAddress.Text = paymentAddress;

            if (invoiceNumber.Length > 0)
            {
                textInvoiceNumber.Text = invoiceNumber;
                textWarning.Visibility = Visibility.Hidden;
            }
        }

        public string KeyNamespace
        {
            set { textKeyNamespace.Text = value;  }
        }
        
        public string InvoiceNumber
        {
            get { return textInvoiceNumber.Text;  }
        }

        public string PaymentAddress
        {
            get { return textPaymentAddress.Text;  }
        }

        public string Note
        {
            get { return textNote.Text; }
            set { textNote.Text = value; }
        }

        private void btnAccept_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            textNote.Focus();
        }
    }
}

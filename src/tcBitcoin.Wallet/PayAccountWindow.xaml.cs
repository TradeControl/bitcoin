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
    public partial class PayAccountWindow : Window
    {
        TCBitcoin tcBitcoin;
        AccountKey accountKey;
        vwInvoicedPayments invoice;

        double balance, balanceOutstanding;

        public PayAccountWindow(TCBitcoin bitcoin, AccountKey extendedKey, vwInvoicedPayments invoiceToPay)
        {
            InitializeComponent();
            tcBitcoin = bitcoin;
            accountKey = extendedKey;
            invoice = invoiceToPay;
        }

        public string PaymentAddress
        {
            get { return textAddress.Text; }
            private set { textAddress.Text = value; }
        }

        public decimal AmountToPay
        {
            get
            {
                try
                {
                    return decimal.Parse(textAmountToPay.Text);
                }
                catch
                {
                    return 0;
                }
            }

            private set
            {
                textAmountToPay.Text = $"{value}";
            }
        }

        public int MinerRate
        {
            get
            {
                return int.Parse(textMinerRate.Text);
            }
            private set
            {
                textMinerRate.Text = $"{value}";
            }
        }

        public string TxMessage
        {
            get
            {
                return textMessage.Text;
            }
        }

        public string CashCodeForChange
        {
            get { return TCBitcoin.ExtractKey(textCashCodeForChange.Text); }
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                textKeyName.Text = accountKey.KeyName;
                balance = await tcBitcoin.KeyNameBalance(accountKey.KeyName);
                textBalance.Text = $"{balance} {TCBitcoin.MILLI_BITCOIN_NAME}";
                textAccountName.Text = invoice.AccountName;
                PaymentAddress = invoice.PaymentAddress;
                balanceOutstanding = await tcBitcoin.NodeCash.AccountBalance(invoice.AccountCode);
                textOutstandingAmount.Text = $"{balanceOutstanding} {TCBitcoin.MILLI_BITCOIN_NAME}";
                AmountToPay = balanceOutstanding <= balance ? (decimal)balanceOutstanding : (decimal)balance;

                textCashCodeForChange.ItemsSource = tcBitcoin.NodeCash.vwTransferCashCodes
                    .Where(text => text.CashModeCode == (short)CashMode.Income)
                    .OrderBy(text => text.CashCode)
                    .Select(cash_code => TCBitcoin.EmbedKey(cash_code.CashCode, $"{cash_code.CashDescription} ({cash_code.Category})"))
                    .ToList();

                if (textCashCodeForChange.Items.Count > 0)
                    textCashCodeForChange.SelectedIndex = 0;

                MinerRates rates = new MinerRates((MinerRates.MiningSpeed)Properties.Settings.Default.MinersFeeSpeed);
                MinerRate = rates.GetFees(1);
            }
            catch (Exception err)
            {
                MessageBox.Show($"{err.Message}", $"{err.Source}.{err.TargetSite.Name}", MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
            }
        }

        private void btnPay_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = (double)AmountToPay <= balance && (double)AmountToPay <= balanceOutstanding && AmountToPay > 0;
        }
    }
}

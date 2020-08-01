using NBitcoin.Protocol;
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
    public partial class KeyTransferWindow : Window
    {
        TCBitcoin tcBitcoin;
        AccountKey accountKey;        

        public KeyTransferWindow(TCBitcoin bitcoin, AccountKey key)
        {
            tcBitcoin = bitcoin;
            accountKey = key;

            InitializeComponent();
        }


        public decimal TransferAmount
        {
            get
            {
                try
                { 
                    return decimal.Parse(textAmount.Text);
                }
                catch
                {
                    return 0;
                }
            }

            private set
            {
                textAmount.Text = $"{value}";
            }
        }

        public string KeyNameTo
        {
            get { return textKeyNameTo.Text; }
            private set { textKeyNameTo.Text = value; }
        }

        public string CashCodeFrom
        {
            get { return TCBitcoin.ExtractKey(textCashCodeFrom.Text); }
        }

        public string CashCodeTo
        {
            get { return TCBitcoin.ExtractKey(textCashCodeTo.Text); }
        }

        public int MinerRate
        {
            get
            {
                return int.Parse(textMinersRate.Text);
            }
            private set
            {
                textMinersRate.Text = $"{value}";
            }
        }

        public string TxMessage
        {
            get
            {
                return textMessage.Text;
            }
        }


        private decimal Balance
        {
            get
            {
                try
                {
                    return decimal.Parse(textBalance.Text);
                }
                catch
                {
                    return 0;
                }                
            }

            set
            {
                textBalance.Text = $"{value}";
            }
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                textAmount.Focus();
                textKeyNameFrom.Text = accountKey.KeyName;
                TransferAmount = 0;

                textKeyNameTo.ItemsSource = tcBitcoin.NodeCash.vwNamespaces
                                            .Where(text => text.CashAccountCode == tcBitcoin.CashAccountCode && text.KeyLevel > 0 && text.KeyName != accountKey.KeyName)
                                            .OrderBy(text => text.KeyName)
                                            .Select(text => text.KeyName).ToList<string>();

                if (textKeyNameTo.Items.Count > 0)
                    textKeyNameTo.SelectedIndex = 0;

                textCashCodeFrom.ItemsSource = tcBitcoin.NodeCash.vwTransferCashCodes
                        .Where(text => text.CashModeCode == (short)CashMode.Expense)
                        .OrderBy(text => text.CashCode)
                        .Select(cash_code => TCBitcoin.EmbedKey(cash_code.CashCode, $"{cash_code.CashDescription} ({cash_code.Category})"))
                        .ToList();

                if (textCashCodeFrom.Items.Count > 0)
                    textCashCodeFrom.SelectedIndex = 0;

                textCashCodeTo.ItemsSource = tcBitcoin.NodeCash.vwTransferCashCodes
                    .Where(text => text.CashModeCode == (short)CashMode.Income)
                    .OrderBy(text => text.CashCode)
                    .Select(cash_code => TCBitcoin.EmbedKey(cash_code.CashCode, $"{cash_code.CashDescription} ({cash_code.Category})"))
                    .ToList();

                if (textCashCodeTo.Items.Count > 0)
                    textCashCodeTo.SelectedIndex = 0;

                var balance = await tcBitcoin.KeyNameBalance(accountKey.KeyName);
                Balance = (decimal)balance;

                MinerRates rates = new MinerRates((MinerRates.MiningSpeed)Properties.Settings.Default.MinersFeeSpeed);
                MinerRate = rates.GetFees(1);
            }
            catch (Exception err)
            {
                MessageBox.Show($"{err.Message}", $"{err.Source}.{err.TargetSite.Name}", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnTransfer_Click(object sender, RoutedEventArgs e)
        {
            if (TransferAmount > Balance || TransferAmount == 0 || MinerRate == 0)
            {
                MessageBox.Show(Properties.Resources.InsufficientFunds, Title, MessageBoxButton.OK, MessageBoxImage.Error);
                DialogResult = false;
            }
            else if (CashCodeFrom.Length == 0)
            {
                MessageBox.Show(Properties.Resources.CashCodeMissing, Title, MessageBoxButton.OK, MessageBoxImage.Error);
                DialogResult = false;
            }
            else
                DialogResult = true;

        }
    }
}

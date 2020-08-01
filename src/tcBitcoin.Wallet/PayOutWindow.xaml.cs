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
    public partial class PayOutWindow : Window
    {
        TCBitcoin tcBitcoin;
        AccountKey accountKey;

        public PayOutWindow(TCBitcoin bitcoin, AccountKey key)
        {
            tcBitcoin = bitcoin;
            accountKey = key;

            InitializeComponent();
        }

        #region properties
        public string PaymentAddress 
        {
            get { return textPaymentAddress.Text; }
        }

        public string AccountCode
        {
            get
            {
                if (textAccountCode.SelectedItem != null)
                    return TCBitcoin.ExtractKey((string)textAccountCode.SelectedItem);
                else
                    return string.Empty;
            }
        }

        public string CashCode
        {
            get
            {
                if (textCashCode.SelectedItem != null)
                    return TCBitcoin.ExtractKey((string)textCashCode.SelectedItem);
                else
                    return string.Empty;
            }
        }

        public string TaxCode
        {
            get
            {
                if (textTaxCode.SelectedItem != null)
                    return TCBitcoin.ExtractKey((string)textTaxCode.SelectedItem);
                else
                    return string.Empty;
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
            get { return textTxMessage.Text; }
        }

        public string PaymentReference
        {
            get { return textPaymentReference.Text; }
        }

        public decimal AmountToPay
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
        #endregion

        #region load
        void LoadOrgs()
        {
            try
            {
                CashMode mode = FilterSuppliers.IsChecked == true ? CashMode.Expense : CashMode.Neutral;

                switch (mode)
                {
                    case CashMode.Expense:
                        textAccountCode.ItemsSource = tcBitcoin.NodeCash.vwOrgLists
                            .Where(tb => tb.CashModeCode == (short)mode && (tb.OrganisationStatusCode == 1 || tb.OrganisationStatusCode == 2))
                            .OrderBy(tb => tb.AccountCode)
                            .Select(account => TCBitcoin.EmbedKey(account.AccountCode, $"{account.AccountName} ({account.OrganisationType})"))
                            .ToList();
                        break;
                    default:
                        textAccountCode.ItemsSource = tcBitcoin.NodeCash.vwOrgLists
                            .OrderBy(tb => tb.AccountCode)
                            .Select(account => TCBitcoin.EmbedKey(account.AccountCode, $"{account.AccountName} ({account.OrganisationType})"))
                            .ToList();
                        break;
                }
            }
            catch (Exception err)
            {
                MessageBox.Show($"{err.Message}", $"{err.Source}.{err.TargetSite.Name}", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        void LoadOrg()
        {
            try
            {
                vwOrgList org = tcBitcoin.NodeCash.vwOrgLists
                    .Where(tb => tb.AccountCode == AccountCode)
                    .Select(tb => tb).First();

                vwCashCode cash_code = tcBitcoin.NodeCash.vwCashCodes
                    .Where(tb => tb.CashCode == org.CashCode)
                    .Select(tb => tb)
                    .FirstOrDefault();

                if (cash_code != null)
                    textCashCode.Text = TCBitcoin.EmbedKey(cash_code.CashCode, $"{cash_code.CashDescription} ({cash_code.Category})");

                vwTaxCode tax_code = tcBitcoin.NodeCash.vwTaxCodes
                    .Where(tb => tb.TaxCode == org.TaxCode)
                    .Select(tb => tb)
                    .FirstOrDefault();

                if (tax_code != null)
                    textTaxCode.Text = TCBitcoin.EmbedKey(tax_code.TaxCode, $"{tax_code.TaxDescription} ({tax_code.TaxType})");

            }
            catch (Exception err)
            {
                MessageBox.Show($"{err.Message}", $"{err.Source}.{err.TargetSite.Name}", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                textAmount.Focus();
                textKeyNameFrom.Text = accountKey.KeyName;
                AmountToPay = 0;

                textPaymentAddress.Focus();

                textCashCode.ItemsSource = tcBitcoin.NodeCash.vwCashCodes
                        .Where(tb => tb.CashModeCode == (short)CashMode.Expense)
                        .OrderBy(tb => tb.CashCode)
                        .Select(cash_code => TCBitcoin.EmbedKey(cash_code.CashCode, $"{cash_code.CashDescription} ({cash_code.Category})"))
                        .ToList();

                textTaxCode.ItemsSource = tcBitcoin.NodeCash.vwTaxCodes
                    .OrderBy(tb => tb.TaxCode)
                    .Select(tax_code => TCBitcoin.EmbedKey(tax_code.TaxCode, $"{tax_code.TaxDescription} ({tax_code.TaxType})"))
                    .ToList();

                LoadOrgs();

                MinerRates rates = new MinerRates((MinerRates.MiningSpeed)Properties.Settings.Default.MinersFeeSpeed);
                MinerRate = rates.GetFees(1);

                var balance = await tcBitcoin.KeyNameBalance(accountKey.KeyName);
                Balance = (decimal)balance;

            }
            catch (Exception err)
            {
                MessageBox.Show($"{err.Message}", $"{err.Source}.{err.TargetSite.Name}", MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
            }
        }
        #endregion

        private void btnPay_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (AmountToPay > Balance || AmountToPay == 0 || MinerRate == 0)
                {
                    MessageBox.Show(Properties.Resources.InsufficientFunds, Title, MessageBoxButton.OK, MessageBoxImage.Error);
                    DialogResult = false;
                }
                else if (PaymentAddress.Length == 0)
                {
                    MessageBox.Show(Properties.Resources.AddressMissing, Title, MessageBoxButton.OK, MessageBoxImage.Error);
                    DialogResult = false;
                }
                else if (AccountCode.Length == 0)
                {
                    MessageBox.Show(Properties.Resources.AccountCodeMissing, Title, MessageBoxButton.OK, MessageBoxImage.Error);
                    DialogResult = false;
                }
                else if (CashCode.Length == 0)
                {
                    MessageBox.Show(Properties.Resources.CashCodeMissing, Title, MessageBoxButton.OK, MessageBoxImage.Error);
                    DialogResult = false;
                }
                else
                    DialogResult = true;
            }
            catch (Exception err)
            {
                MessageBox.Show($"{err.Message}", $"{err.Source}.{err.TargetSite.Name}", MessageBoxButton.OK, MessageBoxImage.Error);
                DialogResult = false;
            }
        }

        private void FilterSuppliers_Checked(object sender, RoutedEventArgs e)
        {
            LoadOrgs();
        }

        private void textAccountCode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LoadOrg();
        }
    }
}

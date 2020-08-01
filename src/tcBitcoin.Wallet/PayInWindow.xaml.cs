using NBitcoin;
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
    /// Interaction logic for PayInWindow.xaml
    /// </summary>
    public partial class PayInWindow : Window
    {
        TCBitcoin tcBitcoin;
        fnTxResult txId;

        public PayInWindow(TCBitcoin ttextitcoin, fnTxResult txid)
        {
            tcBitcoin = ttextitcoin;
            txId = txid;

            InitializeComponent();
        }

        #region load
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                dpPaidOn.SelectedDate = DateTime.Today;
                textPaidInValue.Text = $"{txId.MoneyIn - txId.MoneyOut}";

                textUserName.Text = tcBitcoin.NodeCash.vwUserCredentials
                            .Select(tb => tb.UserName)
                            .First();

                textCashCode.ItemsSource = tcBitcoin.NodeCash.vwCashCodes
                        .Where(tb => tb.CashModeCode != (short)CashMode.Expense)
                        .OrderBy(tb => tb.CashCode)
                        .Select(cash_code => TCBitcoin.EmbedKey(cash_code.CashCode, $"{cash_code.CashDescription} ({cash_code.Category})"))
                        .ToList();

                textTaxCode.ItemsSource = tcBitcoin.NodeCash.vwTaxCodes
                        .OrderBy(tb => tb.TaxCode)
                        .Select(tax_code => TCBitcoin.EmbedKey(tax_code.TaxCode, $"{tax_code.TaxDescription} ({tax_code.TaxType})"))
                        .ToList();

                LoadOrgs();

                textPaymentReference.Text = txId.TxMessage;

                textAccountCode.Focus();
            }
            catch (Exception err)
            {
                MessageBox.Show($"{err.Message}", $"{err.Source}.{err.TargetSite.Name}", MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
            }
        }


        void LoadOrgs()
        {
            try
            { 
                CashMode mode = FilterCustomers.IsChecked == true ? CashMode.Income : CashMode.Neutral;

                switch (mode)
                {
                    case CashMode.Income:
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
        #endregion

        #region Properties
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

        public DateTime PaidOn
        {
            get
            {
                if ((DateTime)dpPaidOn.SelectedDate == DateTime.Today)
                    return DateTime.Now;
                else
                    return (DateTime)dpPaidOn.SelectedDate;
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

        public string PaymentReference
        {
            get
            {
                return textPaymentReference.Text;
            }
        }
        #endregion



        private void btnAccept_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }


        private void FilterCustomers_Checked(object sender, RoutedEventArgs e)
        {
            LoadOrgs();
        }

        private void textAccountCode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LoadOrg();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Data.Linq;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

using TradeControl.Node;

namespace TradeControl.Bitcoin
{
    public partial class InvoicesControl : UserControl
    {
        public CashMode Polarity { get; private set; }

        TreeView tvWallet;
        TCBitcoin tcBitcoin;

        public event BalanceEventHandler OnBalance;

        public InvoicesControl(TreeView treeView, TCBitcoin bitcoin, CashMode cashMode)
        {
            tvWallet = treeView;
            tcBitcoin = bitcoin;
            Polarity = cashMode;

            InitializeComponent();
        }

        private AccountKey AccountKey
        {
            get
            {
                return (AccountKey)tvWallet.SelectedItem;
            }
        }

        public void Refresh()
        {
            try
            {
                switch (Polarity)
                {
                    case CashMode.Income:
                        dgInvoices.ItemsSource = tcBitcoin.NodeCash.vwInvoicedReceipts;
                        break;
                    case CashMode.Expense:
                        dgInvoices.ItemsSource = tcBitcoin.NodeCash.vwInvoicedPayments;
                        break;
                }
            }
            catch (Exception err)
            {
                MessageBox.Show($"{err.Message}", $"{err.Source}.{err.TargetSite.Name}", MessageBoxButton.OK, MessageBoxImage.Error);
            }

        }

        private async void PayAccount()
        {
            try
            {
                var invoice = (vwInvoicedPayments)dgInvoices.SelectedItem;
                AccountKey accountKey = (AccountKey)tvWallet.SelectedItem;
                PayAccountWindow payAccount = new PayAccountWindow(tcBitcoin, accountKey, invoice);

                if (payAccount.ShowDialog() == true)
                {
                    Cursor = Cursors.Wait;
                    SpendTx spendTx = await tcBitcoin.PayOutTx(accountKey, payAccount.PaymentAddress,
                                        payAccount.AmountToPay, payAccount.MinerRate, payAccount.TxMessage);

                    if (!spendTx.IsSatisfied)
                    {
                        MessageBox.Show(Properties.Resources.UnsatisfiedPayment, payAccount.Title, MessageBoxButton.OK, MessageBoxImage.Error);
                        tcBitcoin.PayOutCancel(spendTx);
                    }
                    else
                    {
                        Cursor = Cursors.Arrow;
                        SpendConfirmWindow spendDialog = new SpendConfirmWindow(spendTx);

                        if (spendDialog.ShowDialog() == true)
                        {
                            Cursor = Cursors.Wait;

                            bool broadcast = await spendTx.Send();
                            if (broadcast)
                            {
                                if (tcBitcoin.PayAccountBalanceNode(spendTx, invoice.AccountCode, payAccount.CashCodeForChange))
                                {
                                    Refresh();
                                    OnBalance?.Invoke(this, new EventArgs());
                                }
                            }
                        }
                        else
                            tcBitcoin.PayOutCancel(spendTx);
                    }
                }

            }
            catch (Exception err)
            {
                MessageBox.Show($"{err.Message}", $"{err.Source}.{err.TargetSite.Name}", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                Cursor = Cursors.Arrow;
            }
        }

        private void dgInvoices_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            try
            {
                if (dgInvoices.SelectedItem != null && tvWallet.SelectedItem != null)
                {                    
                    bool hasFunds = AccountKey.Parent.GetType() == typeof(AccountKey);

                    dgInvoicesMenuItemNewReceipt.Visibility = Polarity == CashMode.Income ? Visibility.Visible : Visibility.Collapsed;
                    dgInvoicesMenuItemNewReceipt.IsEnabled = hasFunds;
                    dgInvoicesMenuItemAssignReceipt.Visibility = Polarity == CashMode.Income ? Visibility.Visible : Visibility.Collapsed;
                    dgInvoicesMenuItemPayBalance.Visibility = Polarity == CashMode.Expense ? Visibility.Visible : Visibility.Collapsed;
                    dgInvoicesMenuItemPayBalance.IsEnabled = hasFunds;

                    switch (Polarity)
                    {
                        case CashMode.Expense:
                            var expense = (vwInvoicedPayments)dgInvoices.SelectedItem;
                            dgInvoicesMenuItemCopy.IsEnabled = expense.PaymentAddress?.Length > 0;
                            break;
                        case CashMode.Income:
                            var income = (vwInvoicedReceipts)dgInvoices.SelectedItem;
                            dgInvoicesMenuItemCopy.IsEnabled = income.PaymentAddress?.Length > 0;
                            break;
                    }
                }
                else
                {
                    dgInvoicesMenuItemNewReceipt.IsEnabled = false;
                    dgInvoicesMenuItemAssignReceipt.IsEnabled = false;
                    dgInvoicesMenuItemPayBalance.IsEnabled = false;
                    dgInvoicesMenuItemCopy.IsEnabled = false;
                }
            }
            catch (Exception err)
            {
                MessageBox.Show($"{err.Message}", $"{err.Source}.{err.TargetSite.Name}", MessageBoxButton.OK, MessageBoxImage.Error);
            }

        }

        private void MenuItemNewReceipt_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var invoice = (vwInvoicedReceipts)dgInvoices.SelectedItem;
                
                int? addressIndex = 0;
                
                ReceiptKeyWindow newReceipt = new ReceiptKeyWindow(tcBitcoin.NewChangeKey(AccountKey.KeyName, AccountKey.HDPath, CoinChangeType.Receipt, ref addressIndex), invoice.InvoiceNumber);
                newReceipt.KeyNamespace = AccountKey.KeyNamespace;

                if (newReceipt.ShowDialog() == true)
                {
                    if (tcBitcoin.AddChangeKey(AccountKey.KeyName, CoinChangeType.Receipt, newReceipt.PaymentAddress, (int)addressIndex, newReceipt.Note, newReceipt.InvoiceNumber))
                        Refresh();
                }
            }
            catch (Exception err)
            {
                MessageBox.Show($"{err.Message}", $"{err.Source}.{err.TargetSite.Name}", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void MenuItemAssignReceipt_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                AccountKey extendedKey = (AccountKey)tvWallet.SelectedItem;
                var invoice = (vwInvoicedReceipts)dgInvoices.SelectedItem;
                AssignKeyWindow assignKey = new AssignKeyWindow();
                assignKey.InvoiceNumber = invoice.InvoiceNumber;
                assignKey.dgReceiptKeys.ItemsSource = tcBitcoin.NodeCash.fnChangeUnassigned(tcBitcoin.CashAccountCode);

                if (assignKey.ShowDialog() == true)
                {
                    if (tcBitcoin.AssignReceiptKey(assignKey.KeyName, assignKey.PaymentAddress, assignKey.InvoiceNumber, assignKey.Note))
                    {
                        Refresh();
                        OnBalance?.Invoke(this, new EventArgs());
                    }
                }
            }
            catch (Exception err)
            {
                MessageBox.Show($"{err.Message}", $"{err.Source}.{err.TargetSite.Name}", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void MenuItemPayBalance_Click(object sender, RoutedEventArgs e)
        {
            PayAccount();
        }

        private void MenuItemCopy_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Clipboard.Clear();

                switch (Polarity)
                {
                    case CashMode.Expense:
                        var expense = (vwInvoicedPayments)dgInvoices.SelectedItem;
                        Clipboard.SetText(expense.PaymentAddress);
                        break;
                    case CashMode.Income:
                        var income = (vwInvoicedReceipts)dgInvoices.SelectedItem;
                        Clipboard.SetText(income.PaymentAddress); 
                        break;
                }

                
            }
            catch (Exception err)
            {
                MessageBox.Show($"{err.Message}", $"{err.Source}.{err.TargetSite.Name}", MessageBoxButton.OK, MessageBoxImage.Error);
            }

        }
    }
}

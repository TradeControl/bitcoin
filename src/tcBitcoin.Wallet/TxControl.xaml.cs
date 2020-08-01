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
using System.Windows.Navigation;
using System.Windows.Shapes;
using TradeControl.Node;

namespace TradeControl.Bitcoin
{
    public delegate void BalanceEventHandler(object sender, EventArgs e);

    public partial class TxControl : UserControl
    {
        bool filterOff = true;
        TxStatus filterStatus = TxStatus.Received;

        TreeView tvWallet;
        TCBitcoin tcBitcoin;

        public event BalanceEventHandler OnBalance;

        public TxControl(TreeView treeView, TCBitcoin bitcoin)
        {
            InitializeComponent();

            tvWallet = treeView;
            tcBitcoin = bitcoin;
        }

        public void Refresh()
        {
            if (tcBitcoin == null)
                return;

            if (filterOff)
                dgTx.ItemsSource = tcBitcoin.NodeCash.fnTx(tcBitcoin.CashAccountCode, AccountKey.KeyName)
                                        .Select(tb => tb);
            else
                dgTx.ItemsSource = tcBitcoin.NodeCash.fnTx(tcBitcoin.CashAccountCode, AccountKey.KeyName)
                                        .Where(tb => tb.TxStatusCode == (short)filterStatus)
                                        .Select(tb => tb);
        }

        private AccountKey AccountKey
        {
            get
            {
                return (AccountKey)tvWallet.SelectedItem;
            }
        }

        private void dgTx_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            try
            {
                if (dgTx.SelectedItem != null)
                {
                    fnTxResult tx = (fnTxResult)dgTx.SelectedItem;
                    MenuItemPayIn.IsEnabled = tx.TxStatusCode == (short)TxStatus.Received && tx.Confirmations > 0;
                    MenuItemSync.IsEnabled = true;
                    MenuItemCopy.IsEnabled = true;
                    MenuItemProperties.IsEnabled = true;
                }
                else
                {
                    MenuItemPayIn.IsEnabled = false;
                    MenuItemSync.IsEnabled = false;
                    MenuItemCopy.IsEnabled = false;
                    MenuItemProperties.IsEnabled = false;
                }
            }
            catch (Exception err)
            {
                MessageBox.Show($"{err.Message}", $"{err.Source}.{err.TargetSite.Name}", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void FilterReceived_Checked(object sender, RoutedEventArgs e)
        {
            filterOff = false;
            filterStatus = TxStatus.Received;
            Refresh();
        }

        private void FilterUTXO_Checked(object sender, RoutedEventArgs e)
        {
            filterOff = false;
            filterStatus = TxStatus.UTXO;
            Refresh();
        }

        private void FilterSpent_Checked(object sender, RoutedEventArgs e)
        {
            filterOff = false;
            filterStatus = TxStatus.Spent;
            Refresh();
        }

        private void FilterAll_Click(object sender, RoutedEventArgs e)
        {
            filterOff = true;
            Refresh();
        }

        private void MenuItemCopy_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Clipboard.Clear();
                fnTxResult txId = (fnTxResult)dgTx.SelectedItem;
                Clipboard.SetText(txId.TxId);
            }
            catch (Exception err)
            {
                MessageBox.Show($"{err.Message}", $"{err.Source}.{err.TargetSite.Name}", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void MenuItemSync_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Cursor = Cursors.Wait;
                fnTxResult tx = (fnTxResult)dgTx.SelectedItem;
                tcBitcoin.GetStatement(tx.FullHDPath);
                Refresh();
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


        private void MenuItemProperties_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Clipboard.Clear();
                fnTxResult txId = (fnTxResult)dgTx.SelectedItem;
                fnChangeResult change = tcBitcoin.NodeCash.fnChange(txId.CashAccountCode, txId.KeyName, txId.ChangeTypeCode)
                            .Where(tb => tb.PaymentAddress == txId.PaymentAddress)
                            .Select(tb => tb)
                            .First();

                var changeProperties = new ChangePropertiesWindow(change, tcBitcoin);

                if (changeProperties.ShowDialog() == true)
                    Refresh();
            }
            catch (Exception err)
            {
                MessageBox.Show($"{err.Message}", $"{err.Source}.{err.TargetSite.Name}", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void MenuItemPayIn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                fnTxResult txId = (fnTxResult)dgTx.SelectedItem;
                PayInWindow payIn = new PayInWindow(tcBitcoin, txId);

                if (payIn.ShowDialog() == true)
                {
                    if (tcBitcoin.TxPayIn(txId.PaymentAddress, txId.TxId, payIn.AccountCode, payIn.CashCode, payIn.PaidOn, payIn.PaymentReference))
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

    }
}

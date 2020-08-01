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
    /// <summary>
    /// Interaction logic for ChangeControl.xaml
    /// </summary>
    public partial class ChangeControl : UserControl
    {
        public CoinChangeType ChangeType { get; private set; }

        CoinChangeStatus filterStatus = CoinChangeStatus.Paid;
        bool filterOff = true;

        TreeView tvWallet;
        TCBitcoin tcBitcoin;

        public event BalanceEventHandler OnBalance;

        public ChangeControl(TreeView treeView, TCBitcoin bitcoin, CoinChangeType coinChangeType)
        {
            InitializeComponent();

            tvWallet = treeView;
            tcBitcoin = bitcoin;
            ChangeType = coinChangeType;
        }

        public void Refresh()
        {
            if (tcBitcoin == null)
                return;

            if (filterOff)
                dgChange.ItemsSource = tcBitcoin.NodeCash.fnChange(tcBitcoin.CashAccountCode, SelectedKey.KeyName, (short)ChangeType)
                                            .Select(tb => tb);
            else
                dgChange.ItemsSource = tcBitcoin.NodeCash.fnChange(tcBitcoin.CashAccountCode, SelectedKey.KeyName, (short)ChangeType)
                                            .Where(tb => tb.ChangeStatusCode == (short)filterStatus)
                                            .Select(tb => tb);
        }

        private AccountKey SelectedKey
        {
            get
            {
                return (AccountKey)tvWallet.SelectedItem;
            }
        }

        private void dgChange_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            try
            {
                if (dgChange.SelectedItem != null)
                {
                    fnChangeResult change = (fnChangeResult)dgChange.SelectedItem;
                    MenuItemCopy.IsEnabled = true;
                    MenuItemDelete.IsEnabled = change.Balance == 0 && (CoinChangeStatus)change.ChangeStatusCode == CoinChangeStatus.Unused;
                    MenuItemNote.IsEnabled = true;
                    MenuItemProperties.IsEnabled = true;
                }
                else
                {
                    MenuItemCopy.IsEnabled = false;
                    MenuItemDelete.IsEnabled = false;
                    MenuItemNote.IsEnabled = false;
                    MenuItemProperties.IsEnabled = false;
                }
            }
            catch (Exception err)
            {
                MessageBox.Show($"{err.Message}", $"{err.Source}.{err.TargetSite.Name}", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void MenuItemCopy_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Clipboard.Clear();
                fnChangeResult change = (fnChangeResult)dgChange.SelectedItem;
                Clipboard.SetText(change.PaymentAddress);
            }
            catch (Exception err)
            {
                MessageBox.Show($"{err.Message}", $"{err.Source}.{err.TargetSite.Name}", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void MenuItemDelete_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                fnChangeResult change = (fnChangeResult)dgChange.SelectedItem;
                if (tcBitcoin.DeleteReceiptKey(change.PaymentAddress))
                {
                    Refresh();
                    OnBalance?.Invoke(this, new EventArgs());
                }
            }
            catch (Exception err)
            {
                MessageBox.Show($"{err.Message}", $"{err.Source}.{err.TargetSite.Name}", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void MenuItemNote_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                fnChangeResult change = (fnChangeResult)dgChange.SelectedItem;
                ReceiptKeyWindow receiptKeyWindow = new ReceiptKeyWindow(change.PaymentAddress, change.InvoiceNumber == null ? string.Empty : change.InvoiceNumber);
                receiptKeyWindow.KeyNamespace = change.KeyNamespace;
                receiptKeyWindow.Note = change?.Note;

                if (receiptKeyWindow.ShowDialog() == true)
                {
                    if (tcBitcoin.ChangeKeyNote(change.PaymentAddress, receiptKeyWindow.Note))
                        Refresh();
                }
            }
            catch (Exception err)
            {
                MessageBox.Show($"{err.Message}", $"{err.Source}.{err.TargetSite.Name}", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void FilterUnused_Checked(object sender, RoutedEventArgs e)
        {
            filterOff = false;
            filterStatus = CoinChangeStatus.Unused;
            Refresh();
        }

        private void FilterPaid_Checked(object sender, RoutedEventArgs e)
        {
            filterOff = false;
            filterStatus = CoinChangeStatus.Paid;
            Refresh();
        }

        private void FilterSpent_Checked(object sender, RoutedEventArgs e)
        {
            filterOff = false;
            filterStatus = CoinChangeStatus.Spent;
            Refresh();
        }

        private void FilterAll_Checked(object sender, RoutedEventArgs e)
        {
            filterOff = true;
            Refresh();
        }

        private void MenuItemProperties_Click(object sender, RoutedEventArgs e)
        {
            fnChangeResult change = (fnChangeResult)dgChange.SelectedItem;
            ChangePropertiesWindow propertiesWindow = new ChangePropertiesWindow(change, tcBitcoin);

            if (propertiesWindow.ShowDialog() == true)
            {
                Refresh();
                OnBalance?.Invoke(this, new EventArgs());
            }
        }

        private void MenuItemSync_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Cursor = Cursors.Wait;
                fnChangeResult change = (fnChangeResult)dgChange.SelectedItem;
                tcBitcoin.GetStatement(change.FullHDPath);
                Refresh();
                OnBalance?.Invoke(this, new EventArgs());
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


    }
}

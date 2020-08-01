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
    
    public partial class ChangePropertiesWindow : Window
    {
        TCBitcoin tcBitcoin;
        fnChangeResult change;

        public ChangePropertiesWindow(fnChangeResult changeResult, TCBitcoin bitcoin)
        {
            InitializeComponent();
            tcBitcoin = bitcoin;
            change = changeResult;
        }

        private void Refresh()
        {
            try
            {
                ExtKey changeKey = tcBitcoin.GetExtendedKey(change.FullHDPath);

                textKeyNamespace.Text = change.KeyNamespace;
                textKeyPath.Text = change.FullHDPath;
                textChangeStatus.Text = change.ChangeStatus;
                textInvoiceNumber.Text = change.InvoiceNumber;
                textPrivateKey.Text = Properties.Settings.Default.HidePrivateKeys ? "..." : $"{changeKey.PrivateKey.GetWif(tcBitcoin.GetNetwork)}"; ;
                textAddress.Text = $"{changeKey.PrivateKey.GetWif(tcBitcoin.GetNetwork).GetAddress(ScriptPubKeyType.Legacy)}";
                textBalance.Text = $"{change.Balance}";
                textNote.Text = change.Note;

                dgTransactions.ItemsSource = tcBitcoin.NodeCash.fnChangeTx(textAddress.Text)
                                                .OrderByDescending(text => text.TransactedOn)
                                                .Select(text => text);
                    
            }
            catch (Exception err)
            {
                MessageBox.Show($"{err.Message}", $"{err.Source}.{err.TargetSite.Name}", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (Properties.Settings.Default.ChangeWindowHeight > 0)
            {
                Height = Properties.Settings.Default.ChangeWindowHeight;
                Width = Properties.Settings.Default.ChangeWindowWidth;
                Top = Properties.Settings.Default.ChangeWindowTop;
                Left = Properties.Settings.Default.ChangeWindowLeft;
            }

            Refresh();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Properties.Settings.Default.ChangeWindowHeight = Height;
            Properties.Settings.Default.ChangeWindowWidth = Width;
            Properties.Settings.Default.ChangeWindowLeft = Left;
            Properties.Settings.Default.ChangeWindowTop = Top;
            Properties.Settings.Default.Save();
        }

        private void MenuItemCopyTxId_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Clipboard.Clear();
                fnChangeTxResult changeTx = (fnChangeTxResult)dgTransactions.SelectedItem;
                Clipboard.SetText(changeTx.TxId);
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
                tcBitcoin.GetStatement(textKeyPath.Text);
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

    }
}

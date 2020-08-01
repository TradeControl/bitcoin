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
    public partial class SpendConfirmWindow : Window
    {
        SpendTx spendTx;

        public SpendConfirmWindow(SpendTx spend)
        {
            spendTx = spend;
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                textAddressTo.Text = spendTx.ToAddress;
                textKeyName.Text = spendTx.FromKey;
                textAddressChange.Text = spendTx.ChangeAddress;
                textTxMessage.Text = spendTx.TxMessage;
                textTxInAmount.Text = $"{spendTx.TxInAmount.ToUnit(NBitcoin.MoneyUnit.MilliBTC)} {TCBitcoin.MILLI_BITCOIN_NAME}";
                textSpendAmount.Text = $"{spendTx.SpendAmount.ToUnit(NBitcoin.MoneyUnit.MilliBTC)} {TCBitcoin.MILLI_BITCOIN_NAME}";
                textMinersFee.Text = $"{spendTx.MinerFee.ToUnit(NBitcoin.MoneyUnit.MilliBTC)} {TCBitcoin.MILLI_BITCOIN_NAME}";
                textChangeAmount.Text = $"{spendTx.ChangeAmount.ToUnit(NBitcoin.MoneyUnit.MilliBTC)} {TCBitcoin.MILLI_BITCOIN_NAME}";
                textBalance.Text = $"{spendTx.AccountBalance.ToUnit(NBitcoin.MoneyUnit.MilliBTC)} {TCBitcoin.MILLI_BITCOIN_NAME}";
            }
            catch (Exception err)
            {
                MessageBox.Show($"{err.Message}", $"{err.Source}.{err.TargetSite.Name}", MessageBoxButton.OK, MessageBoxImage.Error);
            }

        }

        private void btnSpend_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }
    }
}

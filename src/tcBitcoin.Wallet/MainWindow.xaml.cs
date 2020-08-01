using NBitcoin;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

using TradeControl.Node;

namespace TradeControl.Bitcoin
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                var txIds = new TxControl(tvWallet, tcBitcoin);
                txIds.OnBalance += tvWallet_OnBalance;
                txIds.Visibility = Visibility.Hidden;
                pageTransactions.Content = txIds;                

                var toReceive = new InvoicesControl(tvWallet, tcBitcoin, CashMode.Income);
                toReceive.OnBalance += tvWallet_OnBalance;
                toReceive.Visibility = Visibility.Hidden;
                pageToReceive.Content = toReceive;

                var toPay = new InvoicesControl(tvWallet, tcBitcoin, CashMode.Expense);
                toPay.OnBalance += tvWallet_OnBalance;
                toPay.Visibility = Visibility.Hidden;
                pageToPay.Content = toPay;

                var receiptKeys = new ChangeControl(tvWallet, tcBitcoin, CoinChangeType.Receipt);
                receiptKeys.OnBalance += tvWallet_OnBalance;
                receiptKeys.Visibility = Visibility.Hidden;
                pageReceiptKeys.Content = receiptKeys;

                var changeKeys = new ChangeControl(tvWallet, tcBitcoin, CoinChangeType.Change);
                changeKeys.OnBalance += tvWallet_OnBalance;
                changeKeys.Visibility = Visibility.Hidden;
                pageChangeKeys.Content = changeKeys;

                if (Properties.Settings.Default.APIAddress.Length > 0)
                    tcBitcoin.ApiAddress = new Uri(Properties.Settings.Default.APIAddress);
                tcBitcoin.MinersFee = (MinerRates.MiningSpeed)Properties.Settings.Default.MinersFeeSpeed;

                using (TCNodeConfig nodeConfig = new TCNodeConfig())
                {
                    SqlServerName = nodeConfig.SqlServerName;
                    Authentication = nodeConfig.Authentication;
                    SqlUserName = nodeConfig.SqlUserName;
                    DatabaseName = nodeConfig.DatabaseName;
                }

                if (SqlServerConnect())
                    ConnectionFooter();
                else
                    SetSqlConnection();

                if (Properties.Settings.Default.WindowHeight > 0)
                {
                    Height = Properties.Settings.Default.WindowHeight;
                    Width = Properties.Settings.Default.WindowWidth;
                    Top = Properties.Settings.Default.WindowTop;
                    Left = Properties.Settings.Default.WindowLeft;
                }

            }
            catch (Exception err)
            {
                MessageBox.Show(err.Message, Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Properties.Settings.Default.WindowHeight = Height;
            Properties.Settings.Default.WindowWidth = Width;
            Properties.Settings.Default.WindowLeft = Left;
            Properties.Settings.Default.WindowTop = Top;
            Properties.Settings.Default.Save();
        }

        private async void tvWallet_OnBalance(object sender, EventArgs e)
        {
            await RefreshBalance();
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void MenuItemConnection_Click(object sender, RoutedEventArgs e)
        {
            SetSqlConnection();            
        }

        private void MenuItemNewWallet_Click(object sender, RoutedEventArgs e)
        {
            NewWallet(false);
        }

        private void MenuItemMnemonics_Click(object sender, RoutedEventArgs e)
        {
            NewWallet(true);
        }

        private void MenuItemCloseWallet_Click(object sender, RoutedEventArgs e)
        {
            CloseWallet();
        }

        private void MenuItemSettings_Click(object sender, RoutedEventArgs e)
        {
            OpenSettings();
        }

        private void cbCashAccount_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cbCashAccount.SelectedItem != null)
                OpenCashCode((string)cbCashAccount.SelectedItem);
        }

        private async void tvWallet_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (tvWallet.SelectedItem != null)
            {
                LoadKey(SelectedKey);
                await RefreshBalance();
            }
        }

        private void tvWalletMenuItemNew_Click(object sender, RoutedEventArgs e)
        {
            if (tvWallet.SelectedItem != null)
            {
                NewChildKey(SelectedKey);
            }
        }

        private void tvWalletMenuItemRename_Click(object sender, RoutedEventArgs e)
        {
            if (tvWallet.SelectedItem != null)
            {
                RenameKey(SelectedKey);
            }
        }

        private void tvWalletMenuItemDelete_Click(object sender, RoutedEventArgs e)
        {
            if (tvWallet.SelectedItem != null)
            {
                DeleteKey(SelectedKey);
            }
        }

        private void MenuItemSaveWallet_Click(object sender, RoutedEventArgs e)
        {
            SaveWallet();
        }

        private void MenuItemOpenWallet_Click(object sender, RoutedEventArgs e)
        {
            LoadWallet();
        }

        private void tvWalletMenuItemNewReceipt_Click(object sender, RoutedEventArgs e)
        {
            NewReceipt(SelectedKey);
        }

        private void tvWallet_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            if (tvWallet.SelectedItem != null)
            {
                AccountKey extendedKey = SelectedKey;
                tvWalletMenuItemNewReceipt.IsEnabled = extendedKey.Parent.GetType() == typeof(AccountKey);
                tvWalletMenuItemMiscPayment.IsEnabled = tvWalletMenuItemNewReceipt.IsEnabled;
                tvWalletMenuItemTransfer.IsEnabled = tvWalletMenuItemNewReceipt.IsEnabled;
            }
        }

        private void tvWalletMenuItemMiscPayment_Click(object sender, RoutedEventArgs e)
        {
            MiscellaneousPayment();
        }

        private void tvWalletMenuItemTransfer_Click(object sender, RoutedEventArgs e)
        {
            KeyTransfer();
        }

        private void MenuItemRefresh_Click(object sender, RoutedEventArgs e)
        {
            Refresh();
        }

        object selectedTab = null; //bodge

        private void tabAccountDetails_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (selectedTab != tabAccountDetails.SelectedItem)
            {
                selectedTab = tabAccountDetails.SelectedItem;
                Refresh();
            }
        }

        private void tvWalletMenuItemSendReceive_Click(object sender, RoutedEventArgs e)
        {
            SendReceive();
        }

        private void MenuItemAbout_Click(object sender, RoutedEventArgs e)
        {
            About about = new About();
            about.ShowDialog();
        }
    }
}

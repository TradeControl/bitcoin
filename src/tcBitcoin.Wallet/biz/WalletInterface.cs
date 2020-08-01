using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using TradeControl.Node;

using NBitcoin;
using System.Drawing;
using Brushes = System.Windows.Media.Brushes;
using Microsoft.Win32;
using System.ComponentModel;
using System.Windows.Input;

namespace TradeControl.Bitcoin
{
    public partial class MainWindow : Window
    {
        TCBitcoin tcBitcoin = new TCBitcoin();

        

        #region settings
        private void OpenSettings()
        {
            try
            {
                SettingsWindow settingsWindow = new SettingsWindow();
                if (Properties.Settings.Default.APIAddress.Length > 0)
                    settingsWindow.APIAddress = new Uri(Properties.Settings.Default.APIAddress, UriKind.Absolute);
                settingsWindow.HidePrivateKeys = Properties.Settings.Default.HidePrivateKeys;
                settingsWindow.MinersFee =(MinerRates.MiningSpeed)Properties.Settings.Default.MinersFeeSpeed;

                settingsWindow.ShowDialog();

                var uri = settingsWindow.APIAddress;
                if (uri != null)
                    Properties.Settings.Default.APIAddress = uri.AbsolutePath;                    
                Properties.Settings.Default.HidePrivateKeys = settingsWindow.HidePrivateKeys;
                Properties.Settings.Default.MinersFeeSpeed = (short)settingsWindow.MinersFee;
                Properties.Settings.Default.Save();

                if (Properties.Settings.Default.APIAddress.Length > 0)
                    tcBitcoin.ApiAddress = new Uri(Properties.Settings.Default.APIAddress);

                tcBitcoin.MinersFee = settingsWindow.MinersFee;
            }
            catch (Exception err)
            {
                MessageBox.Show($"{err.Message}", $"{err.Source}.{err.TargetSite.Name}", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        #endregion

        #region tree view
        private void LoadNamespaces()
        {            
            tvWallet.IsEnabled = true;
            tvWallet.BeginInit();

            var rootItem = (AccountKey)tvWallet.Items[0];
            rootItem.IsEnabled = true;
            rootItem.Items.Clear();
            rootItem.Key = tcBitcoin.GetExtendedKey(rootItem.HDPath);

            LoadNamespaceKey(rootItem);

            rootItem.IsSelected = true;

            LoadKey(rootItem);
            tvWallet.EndInit();

            Refresh();
        }


        private void LoadNamespaceKey(AccountKey parentKey)
        {
            var childKeys = tcBitcoin.NodeCash.vwNamespaces
                                .Where(tb => parentKey.HDPath == tb.ParentHDPath && tb.CashAccountCode == tcBitcoin.CashAccountCode)
                                .Select(tb => tb);

            foreach (var key in childKeys)
            {
                AccountKey childKey = new AccountKey(key.ChildHDPath, key.KeyNamespace, key.KeyName, key.ReceiptIndex, key.ChangeIndex);
                childKey.Key = tcBitcoin.GetExtendedKey(childKey.HDPath);                
                parentKey.Items.Add(childKey);
                LoadNamespaceKey(childKey);
            }

            parentKey.IsExpanded = true;
        }
        #endregion

        #region Key
        private void LoadKey(AccountKey keyItem)
        {
            try
            {
                //var keyDetails = (KeyDetailsControl)pageKeyDetails.Content;
                textNamespace.Text = keyItem.KeyNamespace;
                Refresh();
            }
            catch (Exception err)
            {
                MessageBox.Show($"{err.Message}", $"{err.Source}.{err.TargetSite.Name}", MessageBoxButton.OK, MessageBoxImage.Error);
            }

        }

        private void NewChildKey(AccountKey extendedKey)
        {
            try
            {
                NameWindow newNameWindow = new NameWindow();

                newNameWindow.NamespaceKey = extendedKey.KeyNamespace;

                if (newNameWindow.ShowDialog() == true)
                {                    
                    string hdPath = tcBitcoin.NewKeyPath(extendedKey.KeyName, newNameWindow.KeyName);
                    AccountKey newKey = new AccountKey(hdPath, $"{extendedKey.KeyNamespace}.{newNameWindow.KeyName.ToUpper().Replace(' ', '_')}", newNameWindow.KeyName, 0, 0);
                    newKey.Key = tcBitcoin.GetExtendedKey(hdPath);
                    extendedKey.Items.Add(newKey);
                    extendedKey.IsExpanded = true;
                }
            }
            catch (Exception err)
            {
                MessageBox.Show($"{err.Message}", $"{err.Source}.{err.TargetSite.Name}", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RenameKey(AccountKey extendedKey)
        {
            try
            {
                NameWindow newNameWindow = new NameWindow();

                newNameWindow.NamespaceKey = extendedKey.KeyNamespace;
                newNameWindow.KeyName = extendedKey.KeyName;

                if (newNameWindow.ShowDialog() == true)
                {
                    extendedKey.KeyNamespace = tcBitcoin.RenameKey(extendedKey.KeyName, newNameWindow.KeyName);
                    extendedKey.KeyName = newNameWindow.KeyName;
                    textNamespace.Text = extendedKey.KeyNamespace;
                }
            }
            catch (Exception err)
            {
                MessageBox.Show($"{err.Message}", $"{err.Source}.{err.TargetSite.Name}", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeleteKey(AccountKey extendedKey)
        {
            try
            {
                if (MessageBox.Show($"Okay to delete {extendedKey.KeyName} and all of its descendants?", Title, MessageBoxButton.YesNo , MessageBoxImage.Warning, MessageBoxResult.No) == MessageBoxResult.Yes)
                {
                    if (tcBitcoin.DeleteKey(extendedKey.KeyName))
                        LoadNamespaces();
                }
            }
            catch (Exception err)
            {
                MessageBox.Show($"{err.Message}", $"{err.Source}.{err.TargetSite.Name}", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        AccountKey SelectedKey
        {
            get
            {
                if (tvWallet.SelectedItem != null)
                {
                    var selected = tvWallet.SelectedItem;
                    if (selected.GetType() == typeof(AccountKey))
                        return (AccountKey)selected;
                    else
                        return null;
                }
                else
                    return null;
            }
        }

        AccountKey GetKey(string keyName, AccountKey root = null)
        {
            try
            {
                AccountKey accountKey = null;

                if (root == null)
                    root = (AccountKey)tvWallet.Items[0];

                foreach(AccountKey key in root.Items)
                {
                    if (key.KeyName == keyName)
                    {
                        accountKey = key;
                        return accountKey;
                    }
                    else if (accountKey == null)
                        accountKey = GetKey(keyName, key);
                    else
                        return accountKey;
                }

                return accountKey;
            }
            catch (Exception err)
            {
                MessageBox.Show($"{err.Message}", $"{err.Source}.{err.TargetSite.Name}", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
        }
        #endregion

        #region Sql Server
        string SqlServerName { get; set; } = string.Empty;
        AuthenticationMode Authentication { get; set; }
        string SqlUserName { get; set; } = string.Empty;
        string SqlPassword { get; set; } = string.Empty;
        string DatabaseName { get; set; } = string.Empty;

        public void SqlParamaters(string sqlServerName, AuthenticationMode authentication, string sqlUserName, string sqlPassword, string databaseName)
        {
            SqlServerName = sqlServerName;
            Authentication = authentication;
            SqlUserName = sqlUserName;
            SqlPassword = sqlPassword;
            DatabaseName = databaseName;
        }

        void SetSqlConnection()
        {
            SqlConnectWindow connectionWindow = new SqlConnectWindow(this);

            connectionWindow.SqlServerName = SqlServerName;
            connectionWindow.Authentication = Authentication;
            connectionWindow.SqlUserName = SqlUserName;
            connectionWindow.DatabaseName = DatabaseName;

            if (connectionWindow.ShowDialog() == true)
            {
                CloseWallet();
                if (SqlServerConnect())
                    ConnectionFooter();
            }
        }

        bool SqlServerConnect()
        {
            bool isDbConnected = false;

            try
            {
                tvWallet.Items.Clear();
                cbCashAccount.ItemsSource = null;
                textNamespace.Text = string.Empty;
                

                using (TCNodeConfig tcNode = new TCNodeConfig(
                            SqlServerName,
                            Authentication,
                            SqlUserName,
                            DatabaseName,
                            SqlPassword))
                {
                    if (tcNode.Authenticated)
                    {

                        if (tcNode.IsTCNode)
                        {

                            if (tcNode.InstalledVersion < TCBitcoin.WalletNodeVersion)
                            {
                                lbConnection.Foreground = new SolidColorBrush(Colors.Red);
                                lbConnection.Content = string.Format(Properties.Resources.NodeVersionIncompatible, TCBitcoin.WalletNodeVersion.ToString());
                            }
                            else
                            {                               
                                tcBitcoin.NodeCash = new TCNodeCash(tcNode.ConnectionString);
                                cbCashAccount.ItemsSource = tcBitcoin.NodeCash.CashAccountCodes;

                                if (cbCashAccount.Items.Count > 0)
                                    cbCashAccount.Text = tcBitcoin.NodeCash.CashAccountTrade;

                                //InvoicesControl receipts = (InvoicesControl)pageToReceive.Content;
                                //receipts.Refresh();

                                //InvoicesControl payments = (InvoicesControl)pageToPay.Content;
                                //payments.Refresh();

                                lbConnection.Foreground = new SolidColorBrush(Colors.Black);
                                isDbConnected = true;
                            }
                        }
                        else
                        {
                            lbConnection.Foreground = new SolidColorBrush(Colors.Red);
                            lbConnection.Content = Properties.Resources.UnrecognisedDatasource;
                        }
                    }
                    else
                    {
                        lbConnection.Foreground = new SolidColorBrush(Colors.Red);
                        lbConnection.Content = Properties.Resources.ConnectionFailed;
                    }
                }

                if (!isDbConnected)
                    tcBitcoin.NodeCash = null;
            }
            catch (Exception err)
            {
                lbConnection.Content = $"{err.Source}.{err.TargetSite.Name}: {err.Message}";
                lbConnection.Foreground = new SolidColorBrush(Colors.Red);
            }

            return isDbConnected;

        }
        private void ConnectionFooter()
        {
            const string isEmpty = "_";
            string network = Properties.Settings.Default.Network[(int)tcBitcoin.Coin];
            lbConnection.Content = string.Format(Properties.Resources.Connections,
                    SqlServerName.Length > 0 ? SqlServerName : isEmpty, DatabaseName.Length > 0 ? DatabaseName : isEmpty,
                    tcBitcoin.CashAccountCode, network);
            if (DatabaseName.Length > 0)
                Title = $"{Properties.Resources.AppTitle} [{DatabaseName}]";
            else
                Title = Properties.Resources.AppTitle;
        }
        #endregion

        #region wallet
        private void NewWallet(bool recreate)
        {
            try
            {
                MnemomicsWindow mnemomics = new MnemomicsWindow();
                
                if (!recreate)
                    mnemomics.Mnemonic = tcBitcoin.NewMnemonics(Wordlist.English);

                if (mnemomics.ShowDialog() == true)
                {
                    tcBitcoin.LoadWallet(mnemomics.Mnemonic, Wordlist.English);
                    ActivateWallet();
                }
            }
            catch (Exception err)
            {
                MessageBox.Show($"{err.Message}", $"{err.Source}.{err.TargetSite.Name}", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadWallet()
        {
            try
            {
                OpenFileDialog openFileDialog = new OpenFileDialog();

                openFileDialog.FileName = Properties.Settings.Default.WalletFileName.Length == 0 ? tcBitcoin.CashAccountCode : Properties.Settings.Default.WalletFileName;
                openFileDialog.Filter = "All files (*.*) | *.*";
                openFileDialog.CheckFileExists = true;
                openFileDialog.Title = $"Open {tcBitcoin.CashAccountCode}";

                if (openFileDialog.ShowDialog() == true)
                {
                    tcBitcoin.LoadWallet(openFileDialog.FileName);
                    ActivateWallet();

                    Properties.Settings.Default.WalletFileName = openFileDialog.FileName;
                    Properties.Settings.Default.Save();
                }

            }
            catch (Exception err)
            {
                MessageBox.Show($"{err.Message}", $"{err.Source}.{err.TargetSite.Name}", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ActivateWallet()
        {
            MenuItemNewWallet.IsEnabled = false;
            MenuItemMnemonics.IsEnabled = false;
            MenuItemOpenWallet.IsEnabled = false;
            MenuItemCloseWallet.IsEnabled = true;
            MenuItemSaveWallet.IsEnabled = true;

            if (tcBitcoin.IsWalletInitialised)
            {
                LoadNamespaces();
                MenuItemRefresh.IsEnabled = true;
                MenuItemSendReceive.IsEnabled = true;
            }
            else
            {
                MenuItemRefresh.IsEnabled = false;
                MenuItemSendReceive.IsEnabled = false;
            }

        }

        private void OpenCashCode(string cashAccountCode)
        {
            try
            { 
                tvWallet.Items.Clear();

                var root = tcBitcoin.NodeCash.vwNamespaces 
                                .Where(tb => tb.KeyLevel == 0 && tb.CashAccountCode == cashAccountCode)
                                .Select(tb => tb)
                                .First();

                AccountKey rootItem = new AccountKey(root.ChildHDPath, root.KeyNamespace, root.KeyName, root.ReceiptIndex, root.ChangeIndex);
                rootItem.IsEnabled = false;
                tvWallet.Items.Add(rootItem);
                tvWallet.IsEnabled = false;

                tcBitcoin.CashAccountCode = cashAccountCode;
                CloseWallet();
                ConnectionFooter();
            }
            catch (Exception err)
            {
                MessageBox.Show($"{err.Message}", $"{err.Source}.{err.TargetSite.Name}", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveWallet()
        {
            try
            { 
                SaveFileDialog saveFileDialog = new SaveFileDialog();
                saveFileDialog.FileName = Properties.Settings.Default.WalletFileName.Length == 0 ? $"{DatabaseName}_{tcBitcoin.CashAccountCode}" : Properties.Settings.Default.WalletFileName;
                saveFileDialog.Filter = "All files (*.*) | *.*";
                saveFileDialog.CheckFileExists = false;
                saveFileDialog.Title = $"Save {tcBitcoin.CashAccountCode}";

                if (saveFileDialog.ShowDialog() == true)
                {
                    tcBitcoin.SaveWallet(saveFileDialog.FileName);
                    Properties.Settings.Default.WalletFileName = saveFileDialog.FileName;
                    Properties.Settings.Default.Save();
                    MessageBox.Show($"Wallet saved to {saveFileDialog.SafeFileName}", saveFileDialog.Title, MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception err)
            {
                MessageBox.Show($"{err.Message}", $"{err.Source}.{err.TargetSite.Name}", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private void CloseWallet()
        {
            try
            { 
                tcBitcoin.CloseWallet();
                MenuItemNewWallet.IsEnabled = true;
                MenuItemMnemonics.IsEnabled = true;
                MenuItemOpenWallet.IsEnabled = true;
                MenuItemCloseWallet.IsEnabled = false;
                MenuItemSaveWallet.IsEnabled = false;

                if (tvWallet.Items.Count > 0)
                {
                    var rootItem = (AccountKey)tvWallet.Items[0];
                    rootItem.Items.Clear();
                    rootItem.IsEnabled = false;
                }

                tvWallet.IsEnabled = false;

                if (pageTransactions.Content != null)
                {
                    var tx = (TxControl)pageTransactions.Content;
                    tx.Visibility = Visibility.Hidden;
                }

                if (pageToPay.Content != null)
                {
                    var toPay = (InvoicesControl)pageToPay.Content;
                    toPay.Visibility = Visibility.Hidden;
                }

                if (pageToReceive.Content != null)
                {
                    var toReceive = (InvoicesControl)pageToReceive.Content;
                    toReceive.Visibility = Visibility.Hidden;
                }

                if (pageReceiptKeys.Content != null)
                {
                    var receiptKeys = (ChangeControl)pageReceiptKeys.Content;
                    receiptKeys.Visibility = Visibility.Hidden;
                }

                if (pageChangeKeys.Content != null)
                {
                    var changeKeys = (ChangeControl)pageChangeKeys.Content;
                    changeKeys.Visibility = Visibility.Hidden;
                }

            }
            catch (Exception err)
            {
                MessageBox.Show($"{err.Message}", $"{err.Source}.{err.TargetSite.Name}", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        #endregion

        #region refresh
        private void Refresh()
        {
            var tab = tabAccountDetails.SelectedItem;

            if (tab == pageTransactions)
                RefreshTransactions();
            else if (tab == pageToReceive)
                RefreshToReceive();
            else if (tab == pageToPay)
                RefreshToPay();
            else if (tab == pageReceiptKeys)
                RefreshReceiptKeys();
            else if (tab == pageChangeKeys)
                RefreshChangeKeys();

        }

        private void RefreshTransactions()
        {
            if (tcBitcoin.IsWalletInitialised)
            {
                TxControl txIds = (TxControl)pageTransactions.Content;
                txIds.Refresh();

                if (txIds.Visibility != Visibility.Visible)
                    txIds.Visibility = Visibility.Visible;
            }
        }

        private void RefreshToReceive()
        {
            if (tcBitcoin.IsWalletInitialised)
            {
                InvoicesControl receipts = (InvoicesControl)pageToReceive.Content;
                receipts.Refresh();

                if (receipts.Visibility != Visibility.Visible)
                    receipts.Visibility = Visibility.Visible;
            }
        }

        private void RefreshToPay()
        {
            if (tcBitcoin.IsWalletInitialised)
            {
                InvoicesControl payments = (InvoicesControl)pageToPay.Content;
                payments.Refresh();

                if (payments.Visibility != Visibility.Visible)
                    payments.Visibility = Visibility.Visible;                   
            }
        }

        private void RefreshReceiptKeys()
        {
            if (tcBitcoin.IsWalletInitialised)
            {
                ChangeControl receiptKeys = (ChangeControl)pageReceiptKeys.Content;
                receiptKeys.Refresh();

                if (receiptKeys.Visibility != Visibility.Visible)
                    receiptKeys.Visibility = Visibility.Visible;

            }
        }

        private void RefreshChangeKeys()
        {
            if (tcBitcoin.IsWalletInitialised)
            {
                ChangeControl changeKeys = (ChangeControl)pageChangeKeys.Content;
                changeKeys.Refresh();

                if (changeKeys.Visibility != Visibility.Visible)
                    changeKeys.Visibility = Visibility.Visible;
            }
        }

        async Task<bool> RefreshBalance()
        {
            try
            {
                if (SelectedKey != null)
                {
                    var namespaceBalance = await tcBitcoin.NamespaceBalance(SelectedKey.KeyName);
                    textBalance.Text = $"{namespaceBalance} {TCBitcoin.MILLI_BITCOIN_NAME}";
                    var keyBalance = await tcBitcoin.KeyNameBalance(SelectedKey.KeyName);
                    if (keyBalance != namespaceBalance)
                        textBalance.Text = $"({keyBalance}) {namespaceBalance} {TCBitcoin.MILLI_BITCOIN_NAME}";
                }

                return true;
            }
            catch
            {
                textBalance.Text = $"0.0 {TCBitcoin.MILLI_BITCOIN_NAME}";
                return false;
            }
        }
        #endregion

        #region receipts
        private void NewReceipt(AccountKey extendedKey)
        {
            try
            {
                int? addressIndex = 0;
                ReceiptKeyWindow newReceipt = new ReceiptKeyWindow(tcBitcoin.NewChangeKey(extendedKey.KeyName, extendedKey.HDPath, CoinChangeType.Receipt, ref addressIndex));
                newReceipt.KeyNamespace = extendedKey.KeyNamespace;

                if (newReceipt.ShowDialog() == true)
                {
                    if (tcBitcoin.AddReceiptKey(extendedKey.KeyName, newReceipt.PaymentAddress, (int)addressIndex, newReceipt.Note))
                    {
                        if (tabAccountDetails.SelectedItem != pageReceiptKeys)
                            tabAccountDetails.SelectedItem = pageReceiptKeys;
                        else
                            RefreshReceiptKeys();
                    }
                }

            }
            catch (Exception err)
            {
                MessageBox.Show($"{err.Message}", $"{err.Source}.{err.TargetSite.Name}", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SendReceive()
        {
            try
            {
                BackgroundWorker receiver = new BackgroundWorker
                {
                    WorkerReportsProgress = true
                };

                receiver.DoWork += Receiver_DoWork;
                receiver.ProgressChanged += Receiver_ProgressChanged;
                receiver.RunWorkerCompleted += Receiver_RunWorkerCompleted;

                progressBar.Value = 0;
                progressBar.Visibility = Visibility.Visible;
                lbProgressTag.Visibility = Visibility.Visible;

                receiver.RunWorkerAsync(tcBitcoin);
            }
            catch (Exception err)
            {
                MessageBox.Show($"{err.Message}", $"{err.Source}.{err.TargetSite.Name}", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void Receiver_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            try
            {
                progressBar.Visibility = Visibility.Collapsed;
                lbProgressTag.Visibility = Visibility.Collapsed;
                progressBar.Value = 0;
                Refresh();
                await RefreshBalance();
            }
            catch (Exception err)
            {
                MessageBox.Show($"{err.Message}", $"{err.Source}.{err.TargetSite.Name}", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Receiver_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            progressBar.Value = e.ProgressPercentage;
        }

        private void Receiver_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                TCBitcoin bitcoin = (TCBitcoin)e.Argument;
                bitcoin.ReceiveCoins(sender as BackgroundWorker);
            }
            catch (Exception err)
            {
                MessageBox.Show($"{err.Message}", $"{err.Source}.{err.TargetSite.Name}", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region payments and transfers
        private async void MiscellaneousPayment()
        {
            try
            {
                PayOutWindow payOut = new PayOutWindow(tcBitcoin, SelectedKey);

                if (payOut.ShowDialog() == true)
                {
                    Cursor = Cursors.Wait;
                    SpendTx spendTx = await tcBitcoin.PayOutTx(SelectedKey, payOut.PaymentAddress,
                                        payOut.AmountToPay, payOut.MinerRate, payOut.TxMessage);

                    if (!spendTx.IsSatisfied)
                    {
                        MessageBox.Show(Properties.Resources.UnsatisfiedPayment, payOut.Title, MessageBoxButton.OK, MessageBoxImage.Error);
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
                                if (tcBitcoin.PayOutMiscNode(spendTx, payOut.AccountCode, payOut.CashCode, payOut.TaxCode, payOut.PaymentReference))
                                {
                                    Refresh();
                                    await RefreshBalance();
                                }
                            }
                            else
                                tcBitcoin.PayOutCancel(spendTx);
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

        private async void KeyTransfer()
        {
            try
            {
                   
                KeyTransferWindow keyTransfer = new KeyTransferWindow(tcBitcoin, SelectedKey);

                if (keyTransfer.ShowDialog() == true)
                {
                    Cursor = Cursors.Wait;
                    SpendTx spendTx = await tcBitcoin.KeyTransferTx(SelectedKey, GetKey(keyTransfer.KeyNameTo), 
                                        keyTransfer.TransferAmount, keyTransfer.MinerRate, keyTransfer.TxMessage, keyTransfer.CashCodeFrom, keyTransfer.CashCodeTo);

                    if (!spendTx.IsSatisfied)
                    {
                        MessageBox.Show(Properties.Resources.UnsatisfiedPayment, Title, MessageBoxButton.OK, MessageBoxImage.Error);
                        tcBitcoin.KeyTransferCancel(spendTx);
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
                                if (tcBitcoin.KeyTransferNode(spendTx, keyTransfer.KeyNameTo, keyTransfer.CashCodeFrom, keyTransfer.CashCodeTo))
                                {
                                    Refresh();
                                    await RefreshBalance();
                                }
                            }
                        }
                        else
                            tcBitcoin.KeyTransferCancel(spendTx);
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
        #endregion
    }

    public class AccountKey : TreeViewItem
    {
        string key;

        public AccountKey(string hdPath, string keyNamespace, string keyName, int receiptIndex, int changeIndex)
        {

            HDPath = hdPath;
            KeyNamespace = keyNamespace;
            ChangeIndex = changeIndex;
            ReceiptIndex = receiptIndex;
            Padding = new Thickness(2);
            BorderBrush = Brushes.Transparent;
            KeyName = keyName;
        }

        public string HDPath { get; private set; }


        public string KeyNamespace { get; set; }

        public string KeyName 
        {
            get
            {
                return key;
            }
            set
            {
                key = value;
                Header = KeyName;
            }
        }

        public int ReceiptIndex { get; private set; } = 0;

        public int ChangeIndex { get; private set; } = 0;

        public CoinType GetCoinType
        {
            get
            {
                CoinType coinType = (CoinType)Int16.Parse(HDPath.Substring(HDPath.IndexOf('/') + 1, 1));
                return coinType;
            }
        }

        public ExtKey Key { get; set; } = null;
    }
}

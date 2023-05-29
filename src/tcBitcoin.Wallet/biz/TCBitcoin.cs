using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NBitcoin;
using QBitNinja.Client;
using NBitcoin.Protocol;
using QBitNinja.Client.Models;

using TradeControl.Node;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices.WindowsRuntime;
using System.IO;
using System.ComponentModel;
using NBitcoin.Protocol.Connectors;
using System.Reflection;

namespace TradeControl.Bitcoin
{
    public class TxId
    {
        public uint256 TransactionId { get; }

        public TxStatus Status { get; set; } = TxStatus.Received;
        public Money MoneyIn { get; set; } = Money.Zero;
        public Money MoneyOut { get; set; } = Money.Zero;
        public Money Balance { get { return MoneyIn - MoneyOut; } }
        public int Confirmations { get; set; } = 0;
        public string TxMessage { get; set; }

        public TxId(uint256 txid)
        {
            TransactionId = txid;
        }
    }

    public sealed class TCBitcoin : IDisposable
    {
        const string nodeVersion = "3.28.3";       

        static ExtKey hdRoot = null;
        static TCNodeCash nodeCash = null;

        public const string MILLI_BITCOIN_NAME = "mBTC";

        const string SEPARATOR = ": ";
        public static Func<string, string> ExtractKey = source => source.Substring(0, source.IndexOf(SEPARATOR));
        public static Func<string, string, string> EmbedKey = (keyName, keyDescription) => $"{keyName}{SEPARATOR}{keyDescription}";

        public Uri ApiAddress { get; set; } = null;
        public MinerRates.MiningSpeed MinersFee { get; set; } = MinerRates.MiningSpeed.Hour;      
        public CoinType Coin { get; private set; } = Node.CoinType.TestNet;

        string cashAccountCode = string.Empty;        

        #region class
        public TCBitcoin() {}

        public void Dispose()
        {
            hdRoot = null;
        }

        public static SemVer CurrentVersion
        {
            get
            {
                try
                {
                    SemVer version = new SemVer();
                    Assembly assembly = Assembly.GetExecutingAssembly();
                    AssemblyName assemblyName = assembly.GetName();
                    version.FromString(assemblyName.Version.ToString());
                    return version;
                }
                catch
                {
                    return new SemVer();
                }
            }
        }
        #endregion

        #region network
        public Network GetNetwork 
        { 
            get 
            { 
                switch (Coin)
                {
                    case Node.CoinType.Main:
                        return Network.Main;
                    case Node.CoinType.TestNet:
                        return Network.TestNet;
                    default:
                        return null;
                }
            } 
        }

        QBitNinjaClient BlockchainApi
        {
            get
            {
                if (ApiAddress == null)
                    return new QBitNinjaClient(GetNetwork);
                else
                    return new QBitNinjaClient(ApiAddress, GetNetwork);
            }
        }
        #endregion

        #region receive coins
        public bool ReceiveCoins(BackgroundWorker sender)
        {
            try
            {
                float count = 0, percentComplete = 0; 

                var hdPaths = NodeCash.vwChangeCollections.Select(rec => rec);

                foreach (var hdpath in hdPaths)
                {
                    count++;
                    percentComplete = (count / hdPaths.Count()) * 100;
                    sender?.ReportProgress((int)percentComplete);

                    GetStatement(hdpath.FullHDPath);
                }                

                return NodeCash.proc_PaymentPost() == 0; 
            }
            catch (Exception e)
            {
                //ErrorLog(e);
                throw e;
            }
        }

        public void GetStatement(string keyPath)
        {
            ExtKey key = hdRoot.Derive(new KeyPath(keyPath));

            List<TxId> txIds = new List<TxId>();
            List<uint256> outputs = new List<uint256>();

            var script = key.PrivateKey.GetWif(GetNetwork).GetAddress(ScriptPubKeyType.Legacy).ScriptPubKey;
            var paymentAddress = key.PrivateKey.GetWif(GetNetwork).GetAddress(ScriptPubKeyType.Legacy);

            var client = BlockchainApi;
            

            var balance = client.GetBalance(script, false).Result;

            foreach (var op in balance.Operations)
            {
                var transactionResponse = client.GetTransaction(op.TransactionId).Result;

                foreach (var coin in op.ReceivedCoins)
                {
                    if (coin.TxOut.ScriptPubKey == script)
                    {
                        TxId txId = txIds.Where(id => id.TransactionId == op.TransactionId).FirstOrDefault();

                        if (txId == null)
                        {
                            txId = new TxId(op.TransactionId);
                            txId.Confirmations = transactionResponse.Block == null ? 0 : transactionResponse.Block.Confirmations;
                            txIds.Add(txId);
                        }

                        txId.MoneyIn += (Money)coin.Amount;
                    }
                }

                foreach (TxIn input in transactionResponse.Transaction.Inputs)
                {
                    if (input.ScriptSig == script)
                        outputs.Add(input.PrevOut.Hash);
                }

            }

            foreach (var tx in txIds)
            {
                if (outputs.Where(id => id == tx.TransactionId).Any())
                {
                    tx.Status = TxStatus.Spent;
                    tx.MoneyOut = tx.MoneyIn;
                }

                NodeCash.proc_ChangeTxAdd($"{paymentAddress}", $"{tx.TransactionId}", (short)tx.Status, tx.MoneyIn.ToUnit(MoneyUnit.MilliBTC), tx.Confirmations, tx.TxMessage);
            }
        }
        #endregion

        #region inter-key transfers
        public async Task<SpendTx> KeyTransferTx(AccountKey fromKey, AccountKey toKey, decimal transferAmount, int minersRate, string txMessage, string cashCodeFrom, string cashCodeTo)
        {
            try
            {
                int? addressIndex = 0;

                if (!NodeCash.vwTransferCashCodes.Where(tb => tb.CashCode == cashCodeFrom).Any() || !NodeCash.vwTransferCashCodes.Where(tb => tb.CashCode == cashCodeTo).Any())
                    return null;

                string toAddress = NewChangeKey(toKey.KeyName, toKey.HDPath, CoinChangeType.Receipt, ref addressIndex);
                if (!AddReceiptKey(toKey.KeyName, toAddress, (int)addressIndex, txMessage))
                    return null;

                string changeAddress = string.Empty;
                double accountBalance = await KeyNameBalance(fromKey.KeyName);

                if ((decimal)accountBalance > transferAmount)
                {
                    changeAddress = NewChangeKey(fromKey.KeyName, fromKey.HDPath, CoinChangeType.Change, ref addressIndex);

                    if (!AddChangeKey(fromKey.KeyName, changeAddress, (int)addressIndex, null))
                        return null;
                }

                string changePath = $"{fromKey.HDPath}{(short)CoinChangeType.Change}/{addressIndex}/";

                Money spendAmount = new Money(transferAmount, MoneyUnit.MilliBTC);

                SpendTx spend = new SpendTx(BlockchainApi)
                {
                    ToAddress = toAddress,
                    ChangeAddress = changeAddress,
                    FromKey = fromKey.KeyName,
                    ChangeKey = fromKey.KeyName,
                    MinerRate = minersRate,
                    TxMessage = txMessage,
                    TargetAmount = spendAmount,
                    AccountBalance = new Money((decimal)accountBalance, MoneyUnit.MilliBTC)
                };

                var keyAddresses = NodeCash.fnKeyAddresses(CashAccountCode, fromKey.KeyName)
                                    .OrderBy(tb => tb.AddressIndex)
                                    .Select(tb => tb);

                foreach (var key in keyAddresses)
                {
                    spend.AddKey(GetExtendedKey(key.HDPath), GetExtendedKey(changePath));

                    if (spend.IsSatisfied)
                        break;
                }

                return spend;
            }
            catch (Exception err)
            {
                Console.WriteLine($"Error: {err.Message}");
                return null;
            }
        }

        public bool KeyTransferNode(SpendTx spend, string toKey, string cashCodeFrom, string cashCodeTo)
        {
            try
            {
                string paymentRef = string.Empty;

                paymentRef = $"{spend.FromKey} -> {toKey}";

                foreach (var tx in spend.Transactions)
                {
                    Money minerFee = tx.MinerFee;

                    for (int i = 0; i < tx.TxIds.Count; i++)
                    {
                        TxId input = tx.TxIds[i];
                        Money moneyOut = input.MoneyOut;

                        if (i == tx.TxIds.Count - 1)
                            moneyOut -= tx.ChangeAmount;

                        NodeCash.proc_TxPayOutTransfer
                        (
                            tx.PaymentAddress, 
                            $"{input.TransactionId}",
                            moneyOut.ToUnit(MoneyUnit.MilliBTC), 
                            minerFee.ToUnit(MoneyUnit.MilliBTC), 
                            cashCodeFrom, 
                            paymentRef
                        );

                        minerFee = minerFee <= moneyOut ? Money.Zero : minerFee - moneyOut;
                    }

                    foreach(var output in tx.Tx.Outputs)
                    {
                        string toAddress = $"{output.ScriptPubKey.GetDestinationAddress(GetNetwork)}";
                        string toTxId = $"{tx.Tx.GetHash()}";

                        NodeCash.proc_ChangeTxAdd
                        (
                            toAddress,
                            toTxId,
                            (short)TxStatus.UTXO,
                            output.Value.ToUnit(MoneyUnit.MilliBTC),
                            0,
                            spend.TxMessage
                        );

                        if (toAddress == spend.ChangeAddress)
                            paymentRef = $"{spend.FromKey} -> {spend.FromKey}";
                        else
                            paymentRef = $"{spend.FromKey} -> {toKey}";

                        TxPayIn(toAddress, toTxId, string.Empty, cashCodeTo, DateTime.Now, paymentRef);
                    }

                }

                return true;
            }
            catch (Exception err)
            {
                Console.WriteLine($"Error: {err.Message}");
                return false;
            }

        }

        public void KeyTransferCancel(SpendTx spend)
        {
            NodeCash.DeleteChangeKey(spend.ToAddress);
            if (spend.ChangeAddress.Length > 0)
                NodeCash.DeleteChangeKey(spend.ChangeAddress);
        }
        #endregion

        #region pay out
        public async Task<SpendTx> PayOutTx(AccountKey fromKey, string toAddress, decimal payAmount, int minersRate, string txMessage)
        {
            try
            {
                int? addressIndex = 0;

                string changeAddress = string.Empty;
                double accountBalance = await KeyNameBalance(fromKey.KeyName);

                if ((decimal)accountBalance > payAmount)
                {
                    changeAddress = NewChangeKey(fromKey.KeyName, fromKey.HDPath, CoinChangeType.Change, ref addressIndex);

                    if (!AddChangeKey(fromKey.KeyName, changeAddress, (int)addressIndex, null))
                        return null;
                }

                string changePath = $"{fromKey.HDPath}{(short)CoinChangeType.Change}/{addressIndex}/";

                Money spendAmount = new Money(payAmount, MoneyUnit.MilliBTC);

                SpendTx spend = new SpendTx(BlockchainApi)
                {
                    ToAddress = toAddress,
                    ChangeAddress = changeAddress,
                    FromKey = fromKey.KeyName,
                    ChangeKey = fromKey.KeyName,
                    MinerRate = minersRate,
                    TxMessage = txMessage,
                    TargetAmount = spendAmount,
                    AccountBalance = new Money((decimal)accountBalance, MoneyUnit.MilliBTC)
                };

                var keyAddresses = NodeCash.fnKeyAddresses(CashAccountCode, fromKey.KeyName)
                                    .OrderBy(tb => tb.AddressIndex)
                                    .Select(tb => tb);

                foreach (var key in keyAddresses)
                {
                    spend.AddKey(GetExtendedKey(key.HDPath), GetExtendedKey(changePath));

                    if (spend.IsSatisfied)
                        break;
                }

                return spend;
            }
            catch (Exception err)
            {
                Console.WriteLine($"Error: {err.Message}");
                return null;
            }
        }

        public bool PayOutMiscNode(SpendTx spendTx, string accountCode, string cashCode, string taxCode, string paymentRef)
        {
            try
            {
                NodeCash.proc_TxPayOutInvoice(accountCode, cashCode, taxCode, paymentRef, spendTx.SpendAmount.ToUnit(MoneyUnit.MilliBTC));

                foreach (var tx in spendTx.Transactions)
                {
                    Money minerFee = tx.MinerFee;

                    for (int i = 0; i < tx.TxIds.Count; i++)
                    {
                        TxId input = tx.TxIds[i];
                        Money moneyOut = input.MoneyOut;

                        if (i == tx.TxIds.Count - 1)
                            moneyOut -= tx.ChangeAmount;

                        NodeCash.proc_TxPayOutMisc
                        (
                            tx.PaymentAddress,
                            $"{input.TransactionId}",
                            moneyOut.ToUnit(MoneyUnit.MilliBTC),
                            minerFee.ToUnit(MoneyUnit.MilliBTC),
                            accountCode,
                            cashCode, 
                            taxCode, 
                            paymentRef
                        );

                        minerFee = minerFee <= moneyOut ? Money.Zero : minerFee - moneyOut;
                    }

                    foreach (var output in tx.Tx.Outputs)
                    {
                        string toAddress = $"{output.ScriptPubKey.GetDestinationAddress(GetNetwork)}";
                        if (toAddress == spendTx.ChangeAddress)
                        {
                            string toTxId = $"{tx.Tx.GetHash()}";

                            NodeCash.proc_ChangeTxAdd
                            (
                                toAddress,
                                toTxId,
                                (short)TxStatus.UTXO,
                                output.Value.ToUnit(MoneyUnit.MilliBTC),
                                0,
                                spendTx.TxMessage
                            );

                            NodeCash.proc_TxPayInChange(CashAccountCode, toAddress, toTxId, accountCode, cashCode, Properties.Resources.PaymentChange);
                        }
                    }
                }

                return true;
            }
            catch (Exception err)
            {
                Console.WriteLine($"Error: {err.Message}");
                return false;
            }
        }

        public bool PayAccountBalanceNode(SpendTx spendTx, string accountCode, string cashCodeForChange)
        {
            try
            {
                foreach (var tx in spendTx.Transactions)
                {
                    Money minerFee = tx.MinerFee;
                    
                    for (int i = 0; i < tx.TxIds.Count; i++)
                    {
                        TxId input = tx.TxIds[i];
                        Money moneyOut = input.MoneyOut;

                        if (i == tx.TxIds.Count - 1)
                            moneyOut -= tx.ChangeAmount;

                        NodeCash.proc_TxPayAccount
                        (
                            tx.PaymentAddress,
                            $"{input.TransactionId}",
                            moneyOut.ToUnit(MoneyUnit.MilliBTC),
                            minerFee.ToUnit(MoneyUnit.MilliBTC),
                            accountCode
                        );

                        minerFee = minerFee <= moneyOut ? Money.Zero : minerFee - moneyOut;
                    }

                    foreach (var output in tx.Tx.Outputs)
                    {
                        string toAddress = $"{output.ScriptPubKey.GetDestinationAddress(GetNetwork)}";
                        if (toAddress == spendTx.ChangeAddress)
                        { 
                            string toTxId = $"{tx.Tx.GetHash()}";

                            NodeCash.proc_ChangeTxAdd
                            (
                                toAddress,
                                toTxId,
                                (short)TxStatus.UTXO,
                                output.Value.ToUnit(MoneyUnit.MilliBTC),
                                0,
                                spendTx.TxMessage
                            );

                            NodeCash.proc_TxPayInChange(CashAccountCode, toAddress, toTxId, accountCode, cashCodeForChange, Properties.Resources.PaymentChange);
                        }
                    }
                }

                return true;
            }
            catch (Exception err)
            {
                Console.WriteLine($"Error: {err.Message}");
                return false;
            }
        }

        public void PayOutCancel(SpendTx spend)
        {
            if (spend.ChangeAddress.Length > 0)
                NodeCash.DeleteChangeKey(spend.ChangeAddress);
        }
        #endregion

        #region wallet
        public bool IsWalletLoaded { get; private set; } = false;

        public bool IsWalletInitialised
        {
            get
            {
                return (CashAccountCode.Length > 0 && NodeCash != null & IsWalletLoaded);
            }
        }


        public string NewMnemonics(Wordlist wordlist)
        {
            Mnemonic mnemo = new Mnemonic(wordlist, WordCount.Twelve);
            return $"{mnemo}";            
        }

        public ExtKey LoadWallet(string mnemonics, Wordlist wordlist)
        {
            Mnemonic from_mnemo = new Mnemonic($"{mnemonics}", wordlist);
            hdRoot = from_mnemo.DeriveExtKey(null);
            if (hdRoot != null)
                IsWalletLoaded = true;

            return hdRoot;
        }

        public ExtKey LoadWallet(string sourceFile)
        {
            FileInfo info = new FileInfo(sourceFile);
            FileStream sourceStream = new FileStream(sourceFile, FileMode.Open);

            BinaryReader br = new BinaryReader(sourceStream);
            byte[] rootBytes = new byte[info.Length];
            long l;

            for (l = 0; l < info.Length; l++)
                rootBytes[l] = br.ReadByte();

            br.Close();

            hdRoot = new ExtKey();
            hdRoot = ExtKey.CreateFromBytes(rootBytes);

            if (hdRoot != null)
                IsWalletLoaded = true;

            return hdRoot;
        }

        public void SaveWallet(string fileName)
        {
            byte[] rootBytes = hdRoot.ToBytes();

            FileStream targetStream = new FileStream(fileName, FileMode.Create);
            BinaryWriter bw = new BinaryWriter(targetStream);
            for (long l = 0; l < rootBytes.Length; l++)
                bw.Write(rootBytes[l]);

            bw.Close();
        }

        public void CloseWallet()
        {
            hdRoot = null;
            IsWalletLoaded = false;
        }

        #endregion

        #region tcnode
        public static SemVer WalletNodeVersion
        {
            get
            {
                SemVer semVer = new SemVer();
                semVer.FromString(nodeVersion);
                return semVer;
            }
        }

        public TCNodeCash NodeCash
        {
            get 
            {
                nodeCash = new TCNodeCash(nodeCash.Connection.ConnectionString);
                return nodeCash; 
            }
            set
            {
                nodeCash = value;
            }
        }

        public string CashAccountCode 
        { 
            get
            {
                return cashAccountCode;
            }
            set
            {
                Coin = NodeCash.GetCoinType(value);
                cashAccountCode = value;
            }        
        } 

        public async Task<double> NamespaceBalance(string keyName)
        {
            double balance = await NodeCash.NamespaceBalance(CashAccountCode, keyName);
            return balance;
        }

        public async Task<double> KeyNameBalance(string keyName)
        {
            double balance = await NodeCash.KeyNameBalance(CashAccountCode, keyName);
            return balance;
        }

        #endregion

        #region extended keys
        public ExtKey GetExtendedKey(string hdPath)
        {            

            if (IsWalletInitialised)
                return hdRoot.Derive(new KeyPath(hdPath));
            else
                return null;
        }

        public string NewKeyPath(string parentName, string childName)
        {
            string childHDPath = string.Empty;

            NodeCash.proc_AccountKeyAdd(CashAccountCode, parentName, childName, ref childHDPath);
            return childHDPath;
        }

        public string RenameKey(string oldName, string newName)
        {
            string keyNamespace = string.Empty;

            NodeCash.proc_AccountKeyRename(CashAccountCode, oldName, newName, ref keyNamespace);
            return keyNamespace;
        }

        public bool DeleteKey(string keyName)
        {
            return NodeCash.proc_AccountKeyDelete(CashAccountCode, keyName) == 0;
        }
        #endregion

        #region receipt/change keys
        public bool TxPayIn(string paymentAddress, string txId, string accountCode, string cashCode, DateTime paidOn, string paymentReference)
        {
            try
            {
                return NodeCash.TxPayIn(CashAccountCode, paymentAddress, txId, accountCode, cashCode, paidOn, paymentReference);

            }
            catch (Exception err)
            {
                Console.WriteLine($"Error: {err.Message}");
                return false;
            }
        }
        public string NewChangeKey(string keyName, string hdPath, CoinChangeType changeType, ref int? addressIndex)
        {
            if (IsWalletInitialised)
            {
                NodeCash.proc_ChangeAddressIndex(CashAccountCode, keyName, (short)changeType, ref addressIndex);
                string changePath = $"{hdPath}{(short)changeType}/{addressIndex}/";
                ExtKey privateKey = hdRoot.Derive(new KeyPath(changePath));
                return $"{privateKey.PrivateKey.GetWif(GetNetwork).GetAddress(ScriptPubKeyType.Legacy)}";
            }
            else
                return string.Empty;
        }

        public bool AddReceiptKey(string keyName, string paymentAddress, int addressIndex, string note)
        {
            return AddChangeKey(keyName, CoinChangeType.Receipt, paymentAddress, addressIndex, note, null);
        }

        public bool AddChangeKey(string keyName, string paymentAddress, int addressIndex, string note)
        {
            return AddChangeKey(keyName, CoinChangeType.Change, paymentAddress, addressIndex, note, null);
        }

        public bool AddChangeKey(string keyName, CoinChangeType changeType, string paymentAddress, int addressIndex, string note, string invoiceNumber)
        {
             return NodeCash.proc_ChangeNew(CashAccountCode, keyName, (short)changeType, paymentAddress, addressIndex, invoiceNumber, note?.Length == 0 ? null : note) == 0;
        }

        public bool AssignReceiptKey(string keyName, string paymentAddress, string invoiceNumber, string note)
        {
            return NodeCash.proc_ChangeAssign(CashAccountCode, keyName, paymentAddress, invoiceNumber, note) == 0;
        }

        public bool DeleteReceiptKey(string paymentAddress)
        {
            return NodeCash.DeleteChangeKey(paymentAddress);
        }

        public bool ChangeKeyNote(string paymentAddress, string note)
        {
            return NodeCash.ChangeKeyNote(paymentAddress, note);
        }
        #endregion
    }
}

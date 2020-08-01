using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NBitcoin;
using QBitNinja.Client;
using QBitNinja.Client.Models;

namespace TradeControl.Bitcoin
{
    public sealed class SpendTx
    {
        QBitNinjaClient Api { get; }

        Money accountBalance;

        public string ToAddress { get; set; }

        public string TxMessage { get; set; }
        public Money TargetAmount { get; set; }
        public int MinerRate { get; set; }

        public string ChangeAddress { get; set; }
        public string FromKey { get; set; }
        public string ChangeKey { get; set; }

        public Money AccountBalance 
        {
            get { return accountBalance - TxInAmount + ChangeAmount; }
            set { accountBalance = value; }
        }

        public List<KeyTx> Transactions = new List<KeyTx>();

        public SpendTx(QBitNinjaClient api)
        {
            Api = api;
        }


        public Money TxInAmount
        {
            get
            {
                return Transactions.Select(t => t.TxInAmount).Sum();
            }
        }

        public Money MinerFee
        {
            get
            {
                return Transactions.Select(t => t.MinerFee).Sum();
            }
        }

        public Money SpendAmount
        {
            get
            {
                return Transactions.Select(t => t.SpendAmount).Sum();
            }
        }

        public Money ChangeAmount
        {
            get
            {
                return TxInAmount - MinerFee - SpendAmount;
            }
        }

        public bool IsSatisfied
        {
            get
            {
                return ((AccountBalance == new Money(0, MoneyUnit.BTC)) || ((TxInAmount - MinerFee) >= TargetAmount)) && (MinerFee > Money.Zero);
            }
        }

        public void AddKey(ExtKey spendKey, ExtKey changeKey)
        {
            if (!IsSatisfied)
            {
                KeyTx trial = new KeyTx(this, spendKey, changeKey, new Money(250 * MinerRate, MoneyUnit.Satoshi));
                Transactions.Add(new KeyTx(this, spendKey, changeKey, new Money(trial.TransactionSize * MinerRate, MoneyUnit.Satoshi)));
            }
        }

        public Task<bool> Send()
        {
           return Task.Run(() =>
           {
               try
               {
                   foreach (var transaction in Transactions)
                   {
                       BroadcastResponse broadcastResponse = Api.Broadcast(transaction.Tx).Result;

                       if (!broadcastResponse.Success)
                           Console.WriteLine($"Broadcast Error {broadcastResponse.Error.ErrorCode}:{broadcastResponse.Error.Reason}");
                       else
                           transaction.IsBroadcasted = true;
                   }

                   return Transactions.Where(tx => tx.IsBroadcasted == true).Any();
               }
               catch (Exception err)
               {
                   Console.WriteLine($"Broadcast Error: {err.Message}");
                   return false;
               }
           });
        }



        public class KeyTx
        {
            SpendTx Spend { get; }

            List<ICoin> coins = new List<ICoin>();

            public Transaction Tx { get; }

            public List<TxId> TxIds { get; private set; } = new List<TxId>();

            public Money TxInAmount { get { return TxIds.Select(tx => tx.MoneyOut).Sum(); } }

            public Money SpendAmount { get; private set; } = new Money(0, MoneyUnit.BTC);
            public Money MinerFee { get; private set; } = new Money(0, MoneyUnit.BTC);
            public Money ChangeAmount { get; private set; } = new Money(0, MoneyUnit.BTC);

            public string PaymentAddress { get; }
            public bool IsBroadcasted { get; set; } = false;            

            public int TransactionSize  { get { return Tx.ToBytes().Length; } }

            public KeyTx(SpendTx spend, ExtKey spendKey, ExtKey changeKey, Money minerFee)
            {
                Spend = spend;
                MinerFee = minerFee;

                PaymentAddress = spendKey.PrivateKey.GetWif(Spend.Api.Network).GetAddress(ScriptPubKeyType.Legacy).ToString();

                Tx = Transaction.Create(Spend.Api.Network);
                var ReceiverAddress = BitcoinAddress.Create(Spend.ToAddress, Spend.Api.Network);

                Tx.Outputs.Add(SpendAmount, ReceiverAddress.ScriptPubKey);
                Tx.Outputs.Add(ChangeAmount, changeKey.PrivateKey.GetWif(Spend.Api.Network).GetAddress(ScriptPubKeyType.Legacy).ScriptPubKey);

                if (Spend.TxMessage.Length > 0)
                    Tx.Outputs.Add(Money.Zero, TxNullDataTemplate.Instance.GenerateScriptPubKey(Encoding.UTF8.GetBytes(Spend.TxMessage)));

                GetTransactions(spendKey);

                List<TxId> inputs = new List<TxId>();

                foreach (var txId in TxIds)
                {
                    if (!AddTransaction(spendKey, txId))
                        continue;
                    else
                        inputs.Add(txId);

                    var totalSpend = Spend.SpendAmount + TxInAmount - MinerFee;
                    var remainingAmount = Spend.TargetAmount - totalSpend;

                    SpendAmount = remainingAmount > Money.Zero ? TxInAmount - MinerFee : TxInAmount - MinerFee + remainingAmount; 
                    ChangeAmount = TxInAmount - SpendAmount - MinerFee;

                    if (Spend.TargetAmount <= (Spend.TxInAmount + TxInAmount - Spend.MinerFee - MinerFee))
                        break;
                }

                List<TxId> unusedTx = TxIds.Where(tx => !inputs.Contains(tx)).ToList();
                foreach (var tx in unusedTx)
                    TxIds.Remove(tx);

                Tx.Outputs.Clear();
                Tx.Outputs.Add(SpendAmount, ReceiverAddress.ScriptPubKey);

                if (ChangeAmount > Money.Zero)
                    Tx.Outputs.Add(ChangeAmount, changeKey.PrivateKey.GetWif(Spend.Api.Network).GetAddress(ScriptPubKeyType.Legacy).ScriptPubKey);

                if (Spend.TxMessage.Length > 0)
                    Tx.Outputs.Add(Money.Zero, TxNullDataTemplate.Instance.GenerateScriptPubKey(Encoding.UTF8.GetBytes(Spend.TxMessage)));

                Tx.Sign(spendKey.PrivateKey.GetBitcoinSecret(Spend.Api.Network), coins.ToArray());
            }

            bool AddTransaction(ExtKey key, TxId txId)
            {
                bool outputAdded = false;

                var txResponse = Spend.Api.GetTransaction(txId.TransactionId).Result;

                if (txResponse.Block == null)
                    return false;

                var receivedCoins = txResponse.ReceivedCoins;
                OutPoint outPointToSpend = null;

                foreach (var coin in receivedCoins)
                {
                    if (coin.TxOut.ScriptPubKey == key.PrivateKey.ScriptPubKey)
                    {
                        outPointToSpend = coin.Outpoint;
                        txId.MoneyOut += (Money)coin.Amount;
                        coins.Add(coin);
                    }
                }

                if (outPointToSpend != null)
                {
                    Tx.Inputs.Add(
                        new TxIn()
                        {
                            PrevOut = outPointToSpend,
                            ScriptSig = key.PrivateKey.GetWif(Spend.Api.Network).GetAddress(ScriptPubKeyType.Legacy).ScriptPubKey
                        });
                    outputAdded = true;
                }

                return outputAdded;
            }

            void GetTransactions(ExtKey key)
            {
                List<TxId> txIds = new List<TxId>();
                List<uint256> Inputs = new List<uint256>();

                var balance = Spend.Api.GetBalance(key.PrivateKey.GetWif(Spend.Api.Network).GetAddress(ScriptPubKeyType.Legacy).ScriptPubKey, false).Result;

                foreach (var op in balance.Operations)
                {
                    TxId txid = new TxId(op.TransactionId)
                    {
                        Status = Node.TxStatus.Spent                    
                    };

                    foreach (var coin in op.ReceivedCoins)
                        if (coin.TxOut.ScriptPubKey == key.PrivateKey.ScriptPubKey)
                            txid.MoneyIn += (Money)coin.Amount;

                    var transactionResponse = Spend.Api.GetTransaction(op.TransactionId).Result;
                    Transaction transaction = transactionResponse.Transaction;

                    var inputs = transaction.Inputs;

                    foreach (TxIn input in inputs)
                    {
                        OutPoint previousOutpoint = input.PrevOut;
                        if (input.ScriptSig == key.PrivateKey.ScriptPubKey)
                            Inputs.Add(previousOutpoint.Hash);
                    }

                    if (txid.MoneyIn != Money.Zero)
                        txIds.Add(txid);
                }

                foreach (TxId txId in txIds)
                {
                    if (!(Inputs.Contains(txId.TransactionId))) //|| txId.MoneyOut == Money.Zero))
                        TxIds.Add(txId);
                }

            }

        }

    }
}

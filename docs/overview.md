# Overview

Using [bitcoin](https://bitcoincore.org/) as your commercial Unit of Account (UOA).

## Documentation

- [Bitcoin Installation](./installation.md)
- [Bitcoin Tutorial](./bitcoin.md)

## Commerce

Bitcoin is often referred to as Digital Gold. Hence financial markets see it as an asset that can be stored in the vault of their crypto-currency wallets. However, it was originally conceived by its creator as a [Peer-to-Peer Electronic Cash System](https://bitcoin.org/en/bitcoin-paper). Cash is about trade, of which there are two sides: the consumer and the producer. Whilst consumers are individuals, producers are organisations that connect to each other in supply chains. This difference is reflected in how they function operationally and financially. That is why a producer's business bank account is not the same as a consumer's current account, both in terms of ownership and its relation to workflows. Trading exchanges are highly orchestrated and structured around the product or service, not money.

## Wallet

The commercial wallet of Trade Control is not suitable for individual consumers. It is designed specifically for traders, be they self-employed or corporations, and to demonstrate how a world trading platform might work.

Although Trade Control now incorporates a global, decentralised currency, it is principally a production system that provides the key commercial and financial components you need to run a business, legally and practically. If you study [the schema design](https://github.com/tradecontrol/sqlnode) of Trade Control, you will see that it reconciles to [Cash Accounts](https://github.com/tradecontrol/sqlnode/blob/master/src/tcNodeDb/Org/Tables/tbAccount.sql). When the cash account is expressed in the monetary unit of the presiding jurisdiction, it must map to a centralised bank account. By adding asset type accounts, that old warhorse of capitalism, double-entry book-keeping, can be [operationally replaced](https://github.com/tradecontrol/office). Here we replace the UOA as well. When the cash account is re-assigned from a bank to a crypto wallet, the Trade Control node automatically reconciles to that instead. It is therefore able to programmatically control and synchronise its accounts and workflows without third-party involvement.

Because you can [connect Trade Control nodes together](https://github.com/tradecontrol/network) using Ethereum, now Bitcoin as UOA is available, you have the foundation for a distributed world-wide trading platform coded in GNU Open Source software.

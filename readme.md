# Trade Control - Bitcoin as Unit of Account

Trade Control transacts in a country’s prevailing Unit of Account (UOA). This repository, however, maps a bitcoin wallet to the interior of the [Trade Control Node](https://github.com/tradecontrol/tc-nodecore) using [NBitcoin](https://github.com/metacoSA/NBitcoin), such that it too can become a UOA.

## Overview

Bitcoin is often referred to as Digital Gold. Hence financial markets see it as an asset that can be stored in the vault of their crypto-currency wallets. Okay, but it was originally conceived by its creator as a Peer-to-Peer Electronic Cash System. Cash is about trade, of which there are two sides: the consumer and the producer. Whilst consumers are individuals, producers are [organisations](https://github.com/iamonnox/tradecontrol/blob/master/docs/tc_functions.md#organisations) that connect to each other in [supply chains](https://github.com/iamonnox/tradecontrol/blob/master/docs/tc_functions.md#networks). This difference is reflected in how they function operationally and financially. That is why a producer's business bank account is not the same as a consumer's current account, both in terms of ownership and its relation to [workflows](https://github.com/iamonnox/tradecontrol/blob/master/docs/tc_functions.md#workflow). Trading exchanges are highly orchestrated and structured around the product or service, not money. Therefore, the commercial wallet of Trade Control is not suitable for individual consumers, who should use something like Wasabi or Blockchain. It is designed specifically for traders, be they self-employed or corporations, and to demonstrate how a world trading platform might work.

Although Trade Control now incorporates a global, decentralised currency, it is principally a production system that provides the key commercial and financial components you need to run a business, legally and practically. If you study [the schema design](https://github.com/tradecontrol/tc-nodecore) of Trade Control, you will see that from its [first release in March 2008](https://github.com/iamonnox/tradecontrol/blob/master/docs/tc_history.md) it has reconciled to Cash Accounts (now *tbAccount* in the **Org** schema). When the cash account is expressed in the monetary unit of the presiding jurisdiction, it must map to a centralised bank account. By adding capital depreciation with a dummy cash account using accruals, that old warhorse of capitalism, double-entry book-keeping, can be operationally replaced. Here we replace the UOA as well. When the cash account is re-assigned from a bank to a crypto wallet, the Trade Control node automatically reconciles to that instead. It is therefore able to programmatically control and synchronise its accounts and workflows without third-party involvement.

Because you can [connect Trade Control nodes together](https://github.com/tradecontrol/tc-network) using Ethereum, now Bitcoin as UOA is available, you have the foundation for a distributed world-wide trading platform coded in GNU Open Source software.

## Documentation

- [Bitcoin Wallet Demo](docs/tc_bitcoin_demo.md)
- [Commercial Wallets](https://github.com/iamonnox/tradecontrol/blob/master/docs/tc_bitcoin.md) (an article by the author)

## Installation

For the latest changes and current version, consult the [Change Log](changelog.md)

[Bitcoin Wallet Installation](src/installation/tcWalletSetup.zip)

### Dependencies

The Trade Control Wallet is serviced by changes to the following repositories:

- [node](https://github.com/tradecontrol/tc-nodecore) >= 3.28.3
- [network](https://github.com/tradecontrol/tc-network) >= 1.2.0
- [office](https://github.com/tradecontrol/tc-office) >= 3.14.0

## Donations

[![Donate](https://www.paypalobjects.com/en_US/i/btn/btn_donate_SM.gif)](https://www.paypal.com/cgi-bin/webscr?cmd=_s-xclick&hosted_button_id=C55YGUTBJ4N36)

## Licence

The Trade Control Code licence is issued by Trade Control Ltd under a [GNU General Public Licence v3.0](https://www.gnu.org/licenses/gpl-3.0.en.html) 

Trade Control Documentation by Trade Control Ltd is licenced under a [Creative Commons Attribution-ShareAlike 4.0 International License](http://creativecommons.org/licenses/by-sa/4.0/) 

![Creative Commons](https://i.creativecommons.org/l/by-sa/4.0/88x31.png) 




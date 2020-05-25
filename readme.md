# Trade Control - Bitcoin as Unit of Account

Trade Control presently transacts in a country’s prevailing Unit of Account (UOA). This repository will map a bitcoin wallet to the interior of the [Trade Control Node](https://github.com/tradecontrol/tc-nodecore) using [NBitcoin](https://github.com/metacoSA/NBitcoin), such that it too can become a UOA.

## Preamble

Bitcoin is often referred to as Digital Gold. Hence financial markets see it as an asset that can be stored in the vault of their crypto-currency wallets. Okay, but it was originally conceived by its creator as a Peer-to-Peer Electronic Cash System. Cash is about trade, of which there are two sides: the consumer and the producer. Whilst consumers are individuals, producers are [organisations](https://github.com/iamonnox/tradecontrol/blob/master/docs/tc_functions.md#organisations) that connect to each other in [supply chains](https://github.com/iamonnox/tradecontrol/blob/master/docs/tc_functions.md#networks). This difference is reflected in how they function operationally and financially. That is why a producer's business bank account is not the same as a consumer's current account, both in terms of ownership and its relation to [workflows](https://github.com/iamonnox/tradecontrol/blob/master/docs/tc_functions.md#workflow).

Trade Control is a Production System that provides the key commercial and financial components you need to run a business, both legally and practically. If you study [the schema design](https://github.com/tradecontrol/tc-nodecore) of Trade Control, you will see that from its [first release in March 2008](https://github.com/iamonnox/tradecontrol/blob/master/docs/tc_history.md) it has reconciled to Cash Accounts (now *tbAccount* in the **Org** (subjects) schema). The cash account is expressed in the monetary unit of the presiding jurisdiction and must therefore map to a centralised bank account. Because you could add capital depreciation with a dummy cash account using accruals, that old warhorse of capitalism, double-entry book-keeping, can be operationally replaced. What about replacing the UOA as well? If the cash account is re-assigned from a bank to a crypto wallet, the Trade Control node will automatically reconcile to that instead. It could then programmatically control and synchronise its accounts without third-party involvement.

Because you can now [connect Trade Control nodes together](https://github.com/tradecontrol/tc-network) using Ethereum, when bitcoin as UOA is available, you will have the foundation for a distributed world-wide trading platform coded in GNU Open Source software.

## Donations

[![Donate](https://www.paypalobjects.com/en_US/i/btn/btn_donate_SM.gif)](https://www.paypal.com/cgi-bin/webscr?cmd=_s-xclick&hosted_button_id=C55YGUTBJ4N36)

## Licence

The Trade Control Code licence is issued by Trade Control Ltd under a [GNU General Public Licence v3.0](https://www.gnu.org/licenses/gpl-3.0.en.html) 

Trade Control Documentation by Trade Control Ltd is licenced under a [Creative Commons Attribution-ShareAlike 4.0 International License](http://creativecommons.org/licenses/by-sa/4.0/) 

![Creative Commons](https://i.creativecommons.org/l/by-sa/4.0/88x31.png) 

## Author

Ian Monnox
&nbsp; [GitHub](https://github.com/iamonnox)



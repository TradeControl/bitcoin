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

using NBitcoin;

namespace TradeControl.Bitcoin
{
    public partial class SettingsWindow : Window
    {
        public SettingsWindow()
        {
            InitializeComponent();
        }

        public Uri APIAddress
        {
            get
            {
                if (textAPIAddress.Text.Length > 0)
                    return new Uri(textAPIAddress.Text);
                else
                    return null;
            }
            set
            {
                textAPIAddress.Text = value.AbsolutePath;
            }
        }
        
        public bool HidePrivateKeys
        {
            get { return (bool)checkHidePrivateKeys.IsChecked;  }
            set { checkHidePrivateKeys.IsChecked = value; }
        }

        public MinerRates.MiningSpeed MinersFee
        {
            get { return (MinerRates.MiningSpeed)textMinersFee.SelectedIndex; }
            set { textMinersFee.SelectedIndex = (short)value; }

        }
    }
}

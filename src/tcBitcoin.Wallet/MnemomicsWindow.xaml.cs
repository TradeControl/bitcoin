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
    /// <summary>
    /// Interaction logic for MnemomicsWindow.xaml
    /// </summary>
    public partial class MnemomicsWindow : Window
    {
        public MnemomicsWindow()
        {
            InitializeComponent();
            
        }

        public string Mnemonic
        {
            get { return textMnenomics.Text; }
            set 
            {
                textMnenomics.IsReadOnly = true;
                textMnenomics.Text = value; 
            }
        }


        private void btnNew_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            textMnenomics.Focus();
        }
    }
}

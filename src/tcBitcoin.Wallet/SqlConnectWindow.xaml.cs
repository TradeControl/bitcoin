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

using TradeControl.Node;

namespace TradeControl.Bitcoin
{

    public partial class SqlConnectWindow : Window
    {
        MainWindow _main;

        public SqlConnectWindow(MainWindow main)
        {
            InitializeComponent();
            _main = main;
        }

        #region form events

        #endregion

        #region sql properties
        public AuthenticationMode Authentication
        {
            get
            {
                return (AuthenticationMode)textAuthenticationMode.SelectedIndex;
            }
            set
            {
                textAuthenticationMode.SelectedIndex = (int)value;
            }
        }

        public string SqlServerName
        {
            get
            {
                return textSqlServerName.Text;
            }
            set
            {
                textSqlServerName.Text = value;
            }
        }

        public string DatabaseName
        {
            get
            {
                return textDatabaseName.Text;
            }
            set
            {
                textDatabaseName.Text = value;
            }
        }
        public string SqlUserName
        {
            get
            {
                return textSqlUserName.Text;
            }
            set
            {
                textSqlUserName.Text = value;
            }
        }

        public string SqlPassword { get { return textPassword.Password; } }

        #endregion

        #region events
        private void BtnServers_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Cursor = Cursors.Wait;
                textSqlServerName.Items.Clear();

                List<string> localServers = TCNodeConfig.SqlServers;

                if (localServers.Count > 0)
                {
                    foreach (string serverName in localServers)
                        textSqlServerName.Items.Add(serverName);

                    if (textSqlServerName.Text.Length == 0)
                        textSqlServerName.Text = localServers[0];
                }
            }
            catch (Exception err)
            {
                MessageBox.Show(err.Message, Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                Cursor = Cursors.Arrow;
            }
        }

        private void CbDatabaseName_DropDownOpened(object sender, EventArgs e)
        {
            try
            {
                Cursor = Cursors.Wait;
                textDatabaseName.Items.Clear();

                TCNodeConfig tcnode = new TCNodeConfig(
                    this.SqlServerName,
                    this.Authentication,
                    this.SqlUserName,
                    this.DatabaseName,
                    this.SqlPassword
                    );

                List<string> localDatabases = tcnode.SqlDatabases;

                foreach (string database in localDatabases)
                    textDatabaseName.Items.Add(database);
            }
            catch (Exception err)
            {
                MessageBox.Show(err.Message, Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                Cursor = Cursors.Arrow;
            }
        }

        private void BtnTestConnection_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            _main.SqlParamaters(SqlServerName, Authentication, SqlUserName, SqlPassword, DatabaseName);
            Close();
        }

        private void CbAuthenticationMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (gridCredentials != null)
            {
                gridCredentials.IsEnabled = this.Authentication == AuthenticationMode.SqlServer;
            }
        }
        #endregion

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (Authentication == AuthenticationMode.SqlServer)
                textPassword.Focus();
            else
                textSqlServerName.Focus();
        }
    }
}

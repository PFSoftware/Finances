using PFSoftware.Extensions;
using PFSoftware.Extensions.ListViewHelp;
using PFSoftware.Finances.Models.Data;
using PFSoftware.Finances.Views.Accounts;
using PFSoftware.Finances.Views.Categories;
using PFSoftware.Finances.Views.Credit;
using PFSoftware.Finances.Views.Reports;
using PFSoftware.Finances.Views.Transactions;
using PFSoftware.Finances.Models;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace PFSoftware.Finances.Views
{
    /// <summary>Interaction logic for FinancesPage.xaml</summary>
    public partial class FinancesPage
    {
        private List<Account> _allAccounts;
        private ListViewSort _sort = new ListViewSort();

        /// <summary>Refreshes the ListView's ItemsSource.</summary>
        internal void RefreshItemsSource()
        {
            _allAccounts = AppState.AllAccounts;
            LVAccounts.ItemsSource = _allAccounts;
            LVAccounts.Items.Refresh();
        }

        #region Click Methods

        private void BtnBack_Click(object sender, RoutedEventArgs e) => AppState.GoBack();

        private void BtnNewAccount_Click(object sender, RoutedEventArgs e) => AppState.Navigate(new NewAccountPage());

        private void LVAccounts_SelectionChanged(object sender, SelectionChangedEventArgs e) => BtnViewTransactions.IsEnabled = LVAccounts.SelectedIndex >= 0;

        private void BtnManageCategories_Click(object sender, RoutedEventArgs e) => AppState.Navigate(new ManageCategoriesPage());

        private void BtnMonthlyReport_Click(object sender, RoutedEventArgs e) => AppState.Navigate(new MonthlyReportPage());

        private void BtnViewAccount_Click(object sender, RoutedEventArgs e)
        {
            Account selectedAccount = (Account)LVAccounts.SelectedValue;
            ViewAccountPage viewAccountWindow = new ViewAccountPage();
            viewAccountWindow.LoadAccount(selectedAccount);
            AppState.Navigate(viewAccountWindow);
        }

        private void BtnViewCreditScores_Click(object sender, RoutedEventArgs e) =>
            AppState.Navigate(new CreditScorePage());

        private void BtnViewAllTransactions_Click(object sender, RoutedEventArgs e) => AppState.Navigate(new ViewAllTransactionsPage());

        private void LVAccountsColumnHeader_Click(object sender, RoutedEventArgs e) => _sort = Functions.ListViewColumnHeaderClick(sender, _sort, LVAccounts, "#CCCCCC");

        #endregion Click Methods

        #region Page-Manipulation Methods

        public FinancesPage() => InitializeComponent();

        private async void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            if (!AppState.Loaded)
            {
                AppState.FileManagement();
                await AppState.Load();
            }

            RefreshItemsSource();
        }

        #endregion Page-Manipulation Methods
    }
}
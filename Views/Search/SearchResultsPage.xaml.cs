using PFSoftware.Extensions;
using PFSoftware.Extensions.ListViewHelp;
using PFSoftware.Finances.Models;
using PFSoftware.Finances.Models.Data;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace PFSoftware.Finances.Views.Search
{
    /// <summary>Interaction logic for SearchResultsWindow.xaml</summary>
    public partial class SearchResultsPage
    {
        private List<FinancialTransaction> _allTransactions;
        private ListViewSort _sort = new ListViewSort();

        internal void LoadWindow(List<FinancialTransaction> matchingTransactions)
        {
            _allTransactions = matchingTransactions.OrderByDescending(transaction => transaction.Date)
                .ThenByDescending(transaction => transaction.ID).ToList();
            LVTransactions.ItemsSource = _allTransactions;
            TxtCount.Text = $"Transaction Count: {_allTransactions.Count}";
        }

        #region Click

        private void BtnBack_Click(object sender, RoutedEventArgs e) => ClosePage();

        private void LVTransactionsColumnHeader_Click(object sender, RoutedEventArgs e) => _sort =
            Functions.ListViewColumnHeaderClick(sender, _sort, LVTransactions, "#CCCCCC");

        #endregion Click

        #region Page-Manipulation Methods

        /// <summary>Closes the Page.</summary>
        private void ClosePage() => AppState.GoBack();

        public SearchResultsPage() => InitializeComponent();

        #endregion Page-Manipulation Methods
    }
}
using PFSoftware.Extensions;
using PFSoftware.Extensions.ListViewHelp;
using PFSoftware.Finances.Models;
using PFSoftware.Finances.Models.Data;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace PFSoftware.Finances.Views.Transactions
{
    /// <summary>Interaction logic for ViewAllTransactionsWindow.xaml</summary>
    public partial class ViewAllTransactionsPage
    {
        private readonly List<FinancialTransaction> _allTransactions = AppState.AllTransactions.OrderByDescending(transaction => transaction.Date).ThenByDescending(transaction => transaction.ID).ToList();

        private ListViewSort _sort = new ListViewSort();

        #region Click

        private void BtnBack_Click(object sender, RoutedEventArgs e) => ClosePage();

        private void LVTransactionsColumnHeader_Click(object sender, RoutedEventArgs e) => _sort =
            Functions.ListViewColumnHeaderClick(sender, _sort, LVTransactions, "#CCCCCC");

        #endregion Click

        #region Page-Manipulation Methods

        /// <summary>Closes the Page.</summary>
        private void ClosePage() => AppState.GoBack();

        public ViewAllTransactionsPage()
        {
            InitializeComponent();
            LVTransactions.ItemsSource = _allTransactions;
        }

        #endregion Page-Manipulation Methods
    }
}
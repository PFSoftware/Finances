using PFSoftware.Extensions;
using PFSoftware.Extensions.ListViewHelp;
using PFSoftware.Finances.Models;
using PFSoftware.Finances.Models.Data;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;

namespace PFSoftware.Finances.Views.Transactions
{
    /// <summary>Interaction logic for ViewAllTransactionsWindow.xaml</summary>
    public partial class ViewAllTransactionsPage : INotifyPropertyChanged
    {
        //TODO Fix Viewing All Transactions
        //TODO Fix column width for most ListViews
        //TODO Fix Reports

        private readonly List<FinancialTransaction> _allTransactions = AppState.AllTransactions.OrderByDescending(transaction => transaction.Date).ThenByDescending(transaction => transaction.ID).ToList();

        private ListViewSort _sort = new ListViewSort();

        #region Data-Binding

        /// <summary>Event that fires if a Property value has changed so that the UI can properly be updated.</summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>Invokes <see cref="PropertyChangedEventHandler"/> to update the UI when a Property value changes.</summary>
        /// <param name="property">Name of Property whose value has changed</param>
        private void NotifyPropertyChanged(string property) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));

        #endregion Data-Binding

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
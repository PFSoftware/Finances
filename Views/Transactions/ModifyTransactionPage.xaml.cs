﻿using PFSoftware.Extensions;
using PFSoftware.Extensions.DataTypeHelpers;
using PFSoftware.Extensions.Enums;
using PFSoftware.Finances.Models;
using PFSoftware.Finances.Models.Categories;
using PFSoftware.Finances.Models.Data;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace PFSoftware.Finances.Views.Transactions
{
    /// <summary>Interaction logic for ModifyTransactionWindow.xaml</summary>
    public partial class ModifyTransactionPage : INotifyPropertyChanged
    {
        private readonly List<Account> _allAccounts = AppState.AllAccounts;
        private readonly List<Category> _allCategories = AppState.AllCategories;
        private Category _selectedCategory = new Category();
        private Account _selectedAccount = new Account();
        private FinancialTransaction _modifyTransaction = new FinancialTransaction();

        #region Data-Binding

        /// <summary>Event that fires if a Property value has changed so that the UI can properly be updated.</summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>Invokes <see cref="PropertyChangedEventHandler"/> to update the UI when a Property value changes.</summary>
        /// <param name="property">Name of Property whose value has changed</param>
        private void NotifyPropertyChanged(string property) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));

        #endregion Data-Binding

        internal void SetCurrentTransaction(FinancialTransaction setTransaction, Account setAccount)
        {
            TransactionDate.SelectedDate = setTransaction.Date;
            CmbAccount.SelectedValue = setAccount;
            CmbMajorCategory.SelectedItem = _allCategories.Find(category => category.Name == setTransaction.MajorCategory);
            CmbMinorCategory.SelectedItem = setTransaction.MinorCategory;
            TxtPayee.Text = setTransaction.Payee;
            TxtMemo.Text = setTransaction.Memo;
            TxtOutflow.Text = setTransaction.Outflow.ToString(CultureInfo.InvariantCulture);
            TxtInflow.Text = setTransaction.Inflow.ToString(CultureInfo.InvariantCulture);
            _modifyTransaction = setTransaction;
            _selectedAccount = setAccount;
        }

        #region Button-Click Methods

        private async void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            FinancialTransaction newTransaction = new FinancialTransaction(_modifyTransaction.ID,
                DateTimeHelper.Parse(TransactionDate.SelectedDate), TxtPayee.Text,
                CmbMajorCategory.SelectedItem.ToString(), CmbMinorCategory.SelectedItem.ToString(), TxtMemo.Text,
                DecimalHelper.Parse(TxtOutflow.Text), DecimalHelper.Parse(TxtInflow.Text), _selectedAccount.Name);

            if (newTransaction != _modifyTransaction)
            {
                if (newTransaction.Account != _modifyTransaction.Account)
                {
                    int index = _allAccounts.FindIndex(account => account.Name == _modifyTransaction.Account);
                    _allAccounts[index].ModifyTransaction(_allAccounts[index].AllTransactions.IndexOf(_modifyTransaction),
                        newTransaction);
                    index = _allAccounts.FindIndex(account => account.Name == newTransaction.Account);
                    _allAccounts[index].AddTransaction(newTransaction);
                }
                else
                {
                    int index = _allAccounts.FindIndex(account => account.Name == _selectedAccount.Name);
                    _allAccounts[index].ModifyTransaction(_allAccounts[index].AllTransactions.IndexOf(_modifyTransaction), newTransaction);
                }
                if (await AppState.ModifyFinancialTransaction(newTransaction, _modifyTransaction))
                    ClosePage();
                else
                    AppState.DisplayNotification("Unable to modify transaction.", "Personal Tracker");
            }
            else
                AppState.DisplayNotification("This transaction has not been modified.", "Personal Tracker");
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e) => ClosePage();

        #endregion Button-Click Methods

        #region Text/Selection Changed

        /// <summary>Checks whether or not the Submit button should be enabled.</summary>
        private void TextChanged() => BtnSave.IsEnabled = TransactionDate.SelectedDate != null
                && CmbMajorCategory.SelectedIndex >= 0
                && CmbMinorCategory.SelectedIndex >= 0
                && TxtPayee.Text.Length > 0
                && (TxtInflow.Text.Length > 0 || TxtOutflow.Text.Length > 0)
                && CmbAccount.SelectedIndex >= 0;

        private void Txt_TextChanged(object sender, TextChangedEventArgs e) => TextChanged();

        private void TxtInOutflow_TextChanged(object sender, TextChangedEventArgs e)
        {
            Functions.TextBoxTextChanged(sender, KeyType.Decimals);
            TextChanged();
        }

        private void DatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e) => TextChanged();

        private void CmbCategory_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CmbMinorCategory.IsEnabled = CmbMajorCategory.SelectedIndex >= 0;
            _selectedCategory = CmbMajorCategory.SelectedIndex >= 0
                ? (Category)CmbMajorCategory.SelectedItem
                : new Category();

            CmbMinorCategory.ItemsSource = _selectedCategory.MinorCategories;
            TextChanged();
        }

        private void CmbAccount_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedAccount = CmbAccount.SelectedIndex >= 0 ? (Account)CmbAccount.SelectedValue : new Account();
            TextChanged();
        }

        #endregion Text/Selection Changed

        #region Page-Manipulation Methods

        /// <summary>Closes the Page.</summary>
        private void ClosePage() => AppState.GoBack();

        public ModifyTransactionPage()
        {
            InitializeComponent();
            CmbAccount.ItemsSource = _allAccounts;
            CmbMajorCategory.ItemsSource = _allCategories;
            CmbMinorCategory.ItemsSource = _selectedCategory.MinorCategories;
        }

        private void TxtInflowOutflow_PreviewKeyDown(object sender, KeyEventArgs e) => Functions.PreviewKeyDown(e, KeyType.Decimals);

        private void Txt_GotFocus(object sender, RoutedEventArgs e) => Functions.TextBoxGotFocus(sender);

        #endregion Page-Manipulation Methods
    }
}
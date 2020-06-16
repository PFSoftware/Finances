using PFSoftware.Extensions;
using PFSoftware.Extensions.Enums;
using PFSoftware.Finances.Models.Categories;
using PFSoftware.Finances.Models.Data;
using PFSoftware.Finances.Models.Database;
using PFSoftware.Finances.Models.Sorting;
using PFSoftware.Finances.Views;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;
using System.Windows;
using System.Windows.Controls;

namespace PFSoftware.Finances.Models
{
    public static class AppState
    {
        public static readonly SQLiteDatabaseInteraction DatabaseInteraction = new SQLiteDatabaseInteraction();
        public static List<Account> AllAccounts = new List<Account>();
        public static List<string> AllAccountTypes = new List<string>();
        public static List<Category> AllCategories = new List<Category>();
        public static List<FinancialTransaction> AllTransactions = new List<FinancialTransaction>();
        public static List<Month> AllMonths = new List<Month>();
        public static List<Year> AllYears = new List<Year>();
        public static bool Loaded = false;

        #region Navigation

        /// <summary>Instance of MainWindow currently loaded</summary>
        public static MainWindow MainWindow { get; set; }

        /// <summary>Navigates to selected Page.</summary>
        /// <param name="newPage">Page to navigate to.</param>
        public static void Navigate(Page newPage) => MainWindow.MainFrame.Navigate(newPage);

        /// <summary>Navigates to the previous Page.</summary>
        public static void GoBack()
        {
            if (MainWindow.MainFrame.CanGoBack)
                MainWindow.MainFrame.GoBack();
        }

        #endregion Navigation

        /// <summary>Handles verification of required files.</summary>
        internal static void FileManagement()
        {
            if (!Directory.Exists(AppData.Location))
                Directory.CreateDirectory(AppData.Location);
            DatabaseInteraction.VerifyDatabaseIntegrity();
        }

        /// <summary>Loads all necessary information from the database.</summary>
        internal static async Task Load()
        {
            AllAccounts = await DatabaseInteraction.LoadAccounts();
            AllAccountTypes = await DatabaseInteraction.LoadAccountTypes();
            AllCategories = await DatabaseInteraction.LoadCategories();
            foreach (Account account in AllAccounts)
                foreach (FinancialTransaction trans in account.AllTransactions)
                    AllTransactions.Add(trans);

            AllAccountTypes.Sort();
            AllTransactions = AllTransactions.OrderByDescending(transaction => transaction.Date).ThenByDescending(transaction => transaction.ID).ToList();
            LoadMonths();
            LoadYears();
            Loaded = true;
        }

        #region Finances

        #region Account Manipulation

        /// <summary>Adds an account to the database.</summary>
        /// <param name="newAccount">Account to be added</param>
        /// <returns>Returns true if successful</returns>
        public static async Task<bool> AddAccount(Account newAccount)
        {
            bool success = false;
            if (await DatabaseInteraction.AddAccount(newAccount))
            {
                AllAccounts.Add(newAccount);
                AllAccounts = AllAccounts.OrderBy(account => account.Name).ToList();
                AllTransactions.Add(newAccount.AllTransactions[0]);
                AllTransactions = AllTransactions.OrderByDescending(transaction => transaction.Date).ThenByDescending(transaction => transaction.ID).ToList();
                success = true;
            }

            return success;
        }

        /// <summary>Deletes an account from the database.</summary>
        /// <param name="account">Account to be deleted</param>
        /// <returns>Returns true if successful</returns>
        public static async Task<bool> DeleteAccount(Account account)
        {
            bool success = false;
            if (await DatabaseInteraction.DeleteAccount(account))
            {
                foreach (FinancialTransaction transaction in account.AllTransactions)
                    AllTransactions.Remove(transaction);

                AllAccounts.Remove(account);
                success = true;
            }

            return success;
        }

        /// <summary>Renames an account in the database.</summary>
        /// <param name="account">Account to be renamed</param>
        /// <param name="newAccountName">New account's name</param>
        /// <returns>Returns true if successful</returns>
        public static async Task<bool> RenameAccount(Account account, string newAccountName)
        {
            bool success = false;
            string oldAccountName = account.Name;
            if (await DatabaseInteraction.RenameAccount(account, newAccountName))
            {
                account.Name = newAccountName;

                foreach (FinancialTransaction transaction in AllTransactions)
                {
                    if (transaction.Account == oldAccountName)
                        transaction.Account = newAccountName;
                }
                success = true;
            }

            return success;
        }

        #endregion Account Manipulation

        #region Category Management

        /// <summary>Inserts a new Category into the database.</summary>
        /// <param name="selectedCategory">Selected Major Category</param>
        /// <param name="newName">Name for new Category</param>
        /// <param name="isMajor">Is the category being added a Major Category?</param>
        /// <returns>Returns true if successful.</returns>
        public static async Task<bool> AddCategory(Category selectedCategory, string newName, bool isMajor)
        {
            bool success = false;
            if (await DatabaseInteraction.AddCategory(selectedCategory, newName, isMajor))
            {
                if (isMajor)
                {
                    AllCategories.Add(new Category(newName, new List<string>()));
                }
                else
                    selectedCategory.MinorCategories.Add(newName);

                AllCategories = AllCategories.OrderBy(category => category.Name).ToList();
                success = true;
            }

            return success;
        }

        /// <summary>Rename a category in the database.</summary>
        /// <param name="selectedCategory">Category to rename</param>
        /// <param name="newName">New name of the Category</param>
        /// <param name="oldName">Old name of the Category</param>
        /// <param name="isMajor">Is the category being renamed a Major Category?</param>
        /// <returns></returns>
        public static async Task<bool> RenameCategory(Category selectedCategory, string newName, string oldName, bool isMajor)
        {
            bool success = false;
            if (await DatabaseInteraction.RenameCategory(selectedCategory, newName, oldName, isMajor))
            {
                if (isMajor)
                {
                    selectedCategory = AllCategories.Find(category => category.Name == selectedCategory.Name);
                    selectedCategory.Name = newName;
                    AllTransactions.Select(transaction => transaction.MajorCategory == oldName ? newName : oldName).ToList();
                }
                else
                {
                    selectedCategory = AllCategories.Find(category => category.Name == selectedCategory.Name);
                    selectedCategory.MinorCategories.Remove(oldName);
                    selectedCategory.MinorCategories.Add(newName);
                    AllTransactions.Select(transaction => transaction.MinorCategory == oldName ? newName : oldName).ToList();
                }

                AllCategories = AllCategories.OrderBy(category => category.Name).ToList();
                success = true;
            }

            return success;
        }

        /// <summary>Removes a Major Category from the database, as well as removes it from all Transactions which utilize it.</summary>
        /// <param name="selectedCategory">Selected Major Category to delete</param>
        /// <returns>Returns true if operation successful</returns>
        public static async Task<bool> RemoveMajorCategory(Category selectedCategory)
        {
            bool success = false;
            if (await DatabaseInteraction.RemoveMajorCategory(selectedCategory))
            {
                foreach (FinancialTransaction transaction in AllTransactions)
                {
                    if (transaction.MajorCategory == selectedCategory.Name)
                    {
                        transaction.MajorCategory = "";
                        transaction.MinorCategory = "";
                    }
                }

                AllCategories.Remove(AllCategories.Find(category => category.Name == selectedCategory.Name));
                success = true;
            }

            return success;
        }

        /// <summary>Removes a Major Category from the database, as well as removes it from all Transactions which utilize it.</summary>
        /// <param name="selectedCategory">Selected Major Category</param>
        /// <param name="minorCategory">Selected Minor Category to delete</param>
        /// <returns>Returns true if operation successful</returns>
        public static async Task<bool> RemoveMinorCategory(Category selectedCategory, string minorCategory)
        {
            bool success = false;
            if (await DatabaseInteraction.RemoveMinorCategory(selectedCategory, minorCategory))
            {
                foreach (FinancialTransaction transaction in AllTransactions)
                {
                    if (transaction.MajorCategory == selectedCategory.Name && transaction.MinorCategory == minorCategory)
                        transaction.MinorCategory = "";
                }

                selectedCategory = AllCategories.Find(category => category.Name == selectedCategory.Name);
                selectedCategory.MinorCategories.Remove(minorCategory);
                success = true;
            }

            return success;
        }

        #endregion Category Management

        #region Credit Score Management

        /// <summary>Loads all credit scores from the database.</summary>
        /// <returns>List of all credit scores</returns>
        public static Task<List<CreditScore>> LoadCreditScores() => DatabaseInteraction.LoadCreditScores();

        /// <summary>Adds a new credit score to the database.</summary>
        /// <param name="newScore">Score to be added</param>
        /// <returns>True if successful</returns>
        public static Task<bool> AddCreditScore(CreditScore newScore) =>
            DatabaseInteraction.AddCreditScore(newScore);

        /// <summary>Deletes a credit score from the database</summary>
        /// <param name="deleteScore">Score to be deleted</param>
        /// <returns>True if successful</returns>
        public static Task<bool> DeleteCreditScore(CreditScore deleteScore) =>
            DatabaseInteraction.DeleteCreditScore(deleteScore);

        /// <summary>Modifies a credit score in the database.</summary>
        /// <param name="oldScore">Original score</param>
        /// <param name="newScore">Modified score</param>
        /// <returns>True if successful</returns>
        public static Task<bool> ModifyCreditScore(CreditScore oldScore, CreditScore newScore) =>
            DatabaseInteraction.ModifyCreditScore(oldScore, newScore);

        #endregion Credit Score Management

        #region Transaction Manipulation

        /// <summary>Adds a transaction to an account and the database</summary>
        /// <param name="transaction">Transaction to be added</param>
        /// <param name="account">Account the transaction will be added to</param>
        /// <returns>Returns true if successful</returns>
        public static async Task<bool> AddFinancialTransaction(FinancialTransaction transaction, Account account)
        {
            bool success = false;
            if (await DatabaseInteraction.AddFinancialTransaction(transaction, account))
            {
                if (AllMonths.Any(month => month.MonthStart <= transaction.Date && transaction.Date <= month.MonthEnd.Date))
                    AllMonths.Find(month => month.MonthStart <= transaction.Date && transaction.Date <= month.MonthEnd.Date).AddTransaction(transaction);
                else
                {
                    Month newMonth = new Month(new DateTime(transaction.Date.Year, transaction.Date.Month, 1), new List<FinancialTransaction>());
                    newMonth.AddTransaction(transaction);
                    AppState.AllMonths.Add(newMonth);
                }

                AllMonths = AllMonths.OrderByDescending(month => month.FormattedMonth).ToList();
                success = true;
            }
            else
                DisplayNotification("Unable to process transaction.", "Personal Tracker");

            return success;
        }

        /// <summary>Gets the next Transaction ID autoincrement value in the database for the Transactions table.</summary>
        /// <returns>Next Transactions ID value</returns>
        public static Task<int> GetNextFinancialTransactionIndex() => DatabaseInteraction.GetNextFinancialTransactionIndex();

        /// <summary>Modifies the selected Transaction in the database.</summary>
        /// <param name="newTransaction">Transaction to replace the current one in the database</param>
        /// <param name="oldTransaction">Current Transaction in the database</param>
        /// <returns>Returns true if successful</returns>
        public static async Task<bool> ModifyFinancialTransaction(FinancialTransaction newTransaction, FinancialTransaction oldTransaction)
        {
            bool success = false;
            if (await DatabaseInteraction.ModifyFinancialTransaction(newTransaction, oldTransaction))
            {
                AllTransactions[AllTransactions.IndexOf(oldTransaction)] = newTransaction;
                AllTransactions = AllTransactions.OrderByDescending(transaction => transaction.Date).ThenByDescending(transaction => transaction.ID).ToList();
                LoadMonths();
                LoadYears();
                success = true;
            }

            return success;
        }

        /// <summary>Deletes a transaction from the database.</summary>
        /// <param name="transaction">Transaction to be deleted</param>
        /// <param name="account">Account the transaction will be deleted from</param>
        /// <returns>Returns true if successful</returns>
        public static async Task<bool> DeleteFinancialTransaction(FinancialTransaction transaction, Account account)
        {
            bool success = false;
            if (await DatabaseInteraction.DeleteFinancialTransaction(transaction, account))
            {
                AllTransactions.Remove(transaction);
                success = true;
            }

            return success;
        }

        #endregion Transaction Manipulation

        #endregion Finances

        #region Load

        /// <summary>Loads all the <see cref="Month"/>s from AllTransactions.</summary>
        public static void LoadMonths()
        {
            AllMonths.Clear();

            if (AllTransactions.Count > 0)
            {
                int months = ((DateTime.Now.Year - AllTransactions[AllTransactions.Count - 1].Date.Year) * 12) + DateTime.Now.Month - AllTransactions[AllTransactions.Count - 1].Date.Month;
                DateTime startMonth = new DateTime(AllTransactions[AllTransactions.Count - 1].Date.Year, AllTransactions[AllTransactions.Count - 1].Date.Month, 1);

                int start = 0;
                do
                {
                    AllMonths.Add(new Month(startMonth.AddMonths(start), new List<FinancialTransaction>()));
                    start++;
                }
                while (start <= months);

                foreach (FinancialTransaction transaction in AllTransactions)
                {
                    AllMonths.Find(month => month.MonthStart <= transaction.Date && transaction.Date <= month.MonthEnd.Date).AddTransaction(transaction);
                }

                AllMonths = AllMonths.OrderByDescending(month => month.FormattedMonth).ToList();
            }
        }

        /// <summary>Loads all the <see cref="Year"/>s from AllTransactions.</summary>
        public static void LoadYears()
        {
            AllYears.Clear();

            if (AllTransactions.Count > 0)
            {
                int years = (DateTime.Now.Year - AllTransactions[AllTransactions.Count - 1].Date.Year);
                DateTime startYear = new DateTime(AllTransactions[AllTransactions.Count - 1].Date.Year, 1, 1);

                int start = 0;
                do
                {
                    AllYears.Add(new Year(startYear.AddYears(start), new List<FinancialTransaction>()));
                    start++;
                }
                while (start <= years);

                foreach (FinancialTransaction transaction in AllTransactions)
                {
                    AllYears.Find(year => year.YearStart <= transaction.Date && transaction.Date <= year.YearEnd.Date).AddTransaction(transaction);
                }

                AllYears = AllYears.OrderByDescending(year => year.FormattedYear).ToList();
            }
        }

        #endregion Load

        #region Notification Management

        /// <summary>Displays a new Notification in a thread-safe way.</summary>
        /// <param name="message">Message to be displayed</param>
        /// <param name="title">Title of the Notification window</param>
        public static void DisplayNotification(string message, string title) => Application.Current.Dispatcher.Invoke(
            () => new Notification(message, title, NotificationButton.OK, MainWindow).ShowDialog());

        /// <summary>Displays a new Notification in a thread-safe way and retrieves a boolean result upon its closing.</summary>
        /// <param name="message">Message to be displayed</param>
        /// <param name="title">Title of the Notification window</param>
        /// <returns>Returns value of clicked button on Notification.</returns>
        public static bool YesNoNotification(string message, string title) => Application.Current.Dispatcher.Invoke(() => (new Notification(message, title, NotificationButton.YesNo, MainWindow).ShowDialog() == true));

        #endregion Notification Management
    }
}
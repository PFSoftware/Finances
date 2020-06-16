using PFSoftware.Extensions;
using PFSoftware.Extensions.DatabaseHelp;
using PFSoftware.Extensions.DataTypeHelpers;
using PFSoftware.Finances.Models.Categories;
using PFSoftware.Finances.Models.Data;
using PFSoftware.Finances.Models.Enums;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace PFSoftware.Finances.Models.Database
{
    /// <summary>Represents SQLite implementation of all required database interaction methods.</summary>
    public class SQLiteDatabaseInteraction
    {
        private const string _DATABASENAME = "Finances.sqlite";
        private readonly string _con = $"Data Source={DatabaseLocation}; foreign keys = TRUE; Version = 3;";
        private static readonly string DatabaseLocation = Path.Combine(AppData.Location, _DATABASENAME);

        #region Database Interaction

        /// <summary>Verifies that the requested database exists and that its file size is greater than zero. If not, it extracts the embedded database file to the local output folder.</summary>
        public void VerifyDatabaseIntegrity() => Functions.VerifyFileIntegrity(
            Assembly.GetExecutingAssembly().GetManifestResourceStream($"PFSoftware.Finances.{_DATABASENAME}"), _DATABASENAME, AppData.Location);

        #endregion Database Interaction

        #region Finances

        #region Load

        /// <summary>Loads all <see cref="Account"/>s.</summary>
        /// <returns>Returns all <see cref="Account"/>s</returns>
        public async Task<List<Account>> LoadAccounts()
        {
            List<Account> allAccounts = new List<Account>();
            DataSet ds = await SQLiteHelper.FillDataSet(_con, "SELECT * FROM Accounts");

            if (ds.Tables[0].Rows.Count > 0)
                allAccounts.AddRange(from DataRow dr in ds.Tables[0].Rows select new Account(dr["Name"].ToString(), EnumHelper.Parse<AccountTypes>(dr["Type"].ToString()), new List<FinancialTransaction>()));

            ds = await SQLiteHelper.FillDataSet(_con, "SELECT * FROM FinancialTransactions");
            if (ds.Tables[0].Rows.Count > 0)
            {
                foreach (DataRow dr in ds.Tables[0].Rows)
                {
                    Account selectedAccount = allAccounts.Find(account => account.Name == dr["Account"].ToString());

                    FinancialTransaction newTransaction = new FinancialTransaction(Int32Helper.Parse(dr["ID"]),
                        DateTimeHelper.Parse(dr["Date"]), dr["Payee"].ToString(), dr["MajorCategory"].ToString(),
                        dr["MinorCategory"].ToString(), dr["Memo"].ToString(), DecimalHelper.Parse(dr["Outflow"]),
                        DecimalHelper.Parse(dr["Inflow"]), selectedAccount.Name);
                    selectedAccount.AddTransaction(newTransaction);
                }
            }

            allAccounts = allAccounts.OrderBy(account => account.Name).ToList();
            if (allAccounts.Count > 0)
            {
                foreach (Account account in allAccounts)
                    account.Sort();
            }

            return allAccounts;
        }

        /// <summary>Loads all <see cref="Account"/> types.</summary>
        /// <returns>Returns all <see cref="Account"/> types</returns>
        public async Task<List<string>> LoadAccountTypes()
        {
            List<string> allAccountTypes = new List<string>();
            DataSet ds = await SQLiteHelper.FillDataSet(_con, "SELECT * FROM AccountTypes");

            if (ds.Tables[0].Rows.Count > 0)
                allAccountTypes.AddRange(from DataRow dr in ds.Tables[0].Rows select dr["Name"].ToString());

            return allAccountTypes;
        }

        /// <summary>Loads all <see cref="Category"/>s.</summary>
        /// <returns>Returns all <see cref="Category"/>s</returns>
        public async Task<List<Category>> LoadCategories()
        {
            List<Category> allCategories = new List<Category>();
            DataSet ds = await SQLiteHelper.FillDataSet(_con, "SELECT * FROM MajorCategories");

            if (ds.Tables[0].Rows.Count > 0)
                allCategories.AddRange(from DataRow dr in ds.Tables[0].Rows select new Category(dr["Name"].ToString(), new List<string>()));

            ds = await SQLiteHelper.FillDataSet(_con, "SELECT * FROM MinorCategories");

            if (ds.Tables[0].Rows.Count > 0)
            {
                foreach (DataRow dr in ds.Tables[0].Rows)
                {
                    Category selectedCategory = allCategories.Find(category => category.Name == dr["MajorCategory"].ToString());

                    selectedCategory.MinorCategories.Add(dr["MinorCategory"].ToString());
                }
            }

            allCategories = allCategories.OrderBy(category => category.Name).ToList();

            foreach (Category category in allCategories)
                category.Sort();

            return allCategories;
        }

        /// <summary>Loads all <see cref="CreditScore"/>s from the database.</summary>
        /// <returns>List of all <see cref="CreditScore"/>s</returns>
        public async Task<List<CreditScore>> LoadCreditScores()
        {
            List<CreditScore> scores = new List<CreditScore>();
            DataSet ds = await SQLiteHelper.FillDataSet(_con, "SELECT * FROM CreditScores");
            if (ds.Tables[0].Rows.Count > 0)
            {
                scores.AddRange(from DataRow dr in ds.Tables[0].Rows select new CreditScore(DateTimeHelper.Parse(dr["Date"]), dr["Source"].ToString(), Int32Helper.Parse(dr["Score"]), EnumHelper.Parse<Providers>(dr["Provider"].ToString()), BoolHelper.Parse(dr["FICO"])));
            }

            return scores.OrderByDescending(score => score.Date).ToList();
        }

        #endregion Load

        #region Account Manipulation

        /// <summary>Adds an <see cref="Account"/> to the database.</summary>
        /// <param name="account"><see cref="Account"/> to be added</param>
        /// <returns>Returns true if successful</returns>
        public Task<bool> AddAccount(Account account)
        {
            SQLiteCommand cmd = new SQLiteCommand
            {
                CommandText =
                    "INSERT INTO Accounts([Name], [Type], [Balance])VALUES(@name, @type, @balance)"
            };

            cmd.Parameters.AddWithValue("@name", account.Name);
            cmd.Parameters.AddWithValue("@type", account.AccountType);
            cmd.Parameters.AddWithValue("@balance", account.Balance);
            return SQLiteHelper.ExecuteCommand(_con, cmd);
        }

        /// <summary>Deletes an <see cref="Account"/> from the database.</summary>
        /// <param name="account"><see cref="Account"/> to be deleted</param>
        /// <returns>Returns true if successful</returns>
        public Task<bool> DeleteAccount(Account account)
        {
            SQLiteCommand cmd = new SQLiteCommand { CommandText = "DELETE FROM Accounts WHERE [Name] = @name" };
            cmd.Parameters.AddWithValue("@name", account.Name);

            return SQLiteHelper.ExecuteCommand(_con, cmd);
        }

        /// <summary>Renames an <see cref="Account"/> in the database.</summary>
        /// <param name="account"><see cref="Account"/> to be renamed</param>
        /// <param name="newAccountName">New <see cref="Account"/>'s name</param>
        /// <returns>Returns true if successful</returns>
        public Task<bool> RenameAccount(Account account, string newAccountName)
        {
            string oldAccountName = account.Name;
            SQLiteCommand cmd = new SQLiteCommand
            {
                CommandText = "UPDATE Accounts SET [Name] = @newAccountName WHERE [Name] = @oldAccountName"
            };
            cmd.Parameters.AddWithValue("@newAccountName", newAccountName);
            cmd.Parameters.AddWithValue("@oldAccountName", oldAccountName);

            return SQLiteHelper.ExecuteCommand(_con, cmd);
        }

        #endregion Account Manipulation

        #region Category Management

        /// <summary>Inserts a new <see cref="Category"/> into the database.</summary>
        /// <param name="selectedCategory">Selected Major <see cref="Category"/></param>
        /// <param name="newName">Name for new <see cref="Category"/></param>
        /// <param name="isMajor">Is the <see cref="Category"/> being added a Major <see cref="Category"/>?</param>
        /// <returns>Returns true if successful.</returns>
        public Task<bool> AddCategory(Category selectedCategory, string newName, bool isMajor)
        {
            SQLiteCommand cmd = new SQLiteCommand
            {
                CommandText = isMajor
                    ? "INSERT INTO MajorCategories([Name])VALUES(@majorCategory)"
                    : "INSERT INTO MinorCategories([MajorCategory], [MinorCategory])VALUES(@majorCategory, @minorCategory)"
            };

            if (isMajor)
                cmd.Parameters.AddWithValue("@majorCategory", newName);
            else
            {
                cmd.Parameters.AddWithValue("@majorCategory", selectedCategory.Name);
                cmd.Parameters.AddWithValue("@minorCategory", newName);
            }

            return SQLiteHelper.ExecuteCommand(_con, cmd);
        }

        /// <summary>Renames a <see cref="Category"/> in the database.</summary>
        /// <param name="selectedCategory"><see cref="Category"/> to rename</param>
        /// <param name="newName">New name of the <see cref="Category"/></param>
        /// <param name="oldName">Old name of the <see cref="Category"/></param>
        /// <param name="isMajor">Is the <see cref="Category"/> being renamed a Major <see cref="Category"/>?</param>
        /// <returns>True if successful</returns>
        public Task<bool> RenameCategory(Category selectedCategory, string newName, string oldName, bool isMajor)
        {
            SQLiteCommand cmd = new SQLiteCommand
            {
                CommandText = isMajor
                    ? "UPDATE MajorCategories SET [Name] = @newName WHERE [Name] = @oldName; UPDATE MinorCategories SET [MajorCategory] = @newName WHERE [MajorCategory] = @oldName"
                    : "UPDATE MinorCategories SET [MinorCategory] = @newName WHERE [MinorCategory] = @oldName AND [MajorCategory] = @majorCategory"
            };
            cmd.Parameters.AddWithValue("@newName", newName);
            cmd.Parameters.AddWithValue("@oldName", oldName);
            cmd.Parameters.AddWithValue("@majorCategory", selectedCategory.Name);

            return SQLiteHelper.ExecuteCommand(_con, cmd);
        }

        /// <summary>Removes a Major <see cref="Category"/> from the database, as well as removes it from all <see cref="FinancialTransaction"/>s which utilize it.</summary>
        /// <param name="selectedCategory">Selected Major Category to delete</param>
        /// <returns>Returns true if operation successful</returns>
        public Task<bool> RemoveMajorCategory(Category selectedCategory)
        {
            SQLiteCommand cmd = new SQLiteCommand
            {
                CommandText =
                "DELETE FROM MajorCategories WHERE [Name] = @name; DELETE FROM MinorCategories WHERE [MajorCategory] = @name; UPDATE FinancialTransactions SET [MajorCategory] = @newName AND [MinorCategory] = @newName WHERE [MinorCategory] = @name"
            };
            cmd.Parameters.AddWithValue("@name", selectedCategory.Name);
            cmd.Parameters.AddWithValue("@newName", "");

            return SQLiteHelper.ExecuteCommand(_con, cmd);
        }

        /// <summary>Removes a Minor <see cref="Category"/> from the database, as well as removes it from all <see cref="FinancialTransaction"/>s which utilize it.</summary>
        /// <param name="selectedCategory">Selected Major <see cref="Category"/></param>
        /// <param name="minorCategory">Selected Minor <see cref="Category"/> to delete</param>
        /// <returns>Returns true if operation successful</returns>
        public Task<bool> RemoveMinorCategory(Category selectedCategory, string minorCategory)
        {
            SQLiteCommand cmd = new SQLiteCommand
            {
                CommandText =
                "DELETE FROM MinorCategories WHERE [MajorCategory] = @majorCategory AND [MinorCategory] = @minorCategory; UPDATE FinancialTransactions SET [MinorCategory] = @newMinorName WHERE [MajorCategory] = @majorCategory AND [MinorCategory] = @minorCategory"
            };
            cmd.Parameters.AddWithValue("@majorCategory", selectedCategory.Name);
            cmd.Parameters.AddWithValue("@minorCategory", minorCategory);
            cmd.Parameters.AddWithValue("@newMinorName", "");

            return SQLiteHelper.ExecuteCommand(_con, cmd);
        }

        #endregion Category Management

        #region Credit Score Management

        /// <summary>Adds a new <see cref="CreditScore"/> to the database.</summary>
        /// <param name="newScore"><see cref="CreditScore"/> to be added</param>
        /// <returns>True if successful</returns>
        public Task<bool> AddCreditScore(CreditScore newScore)
        {
            SQLiteCommand cmd = new SQLiteCommand
            {
                CommandText =
                    "INSERT INTO CreditScores([Date], [Source], [Score], [Provider], [FICO])VALUES(@date, @source, @score, @provider, @fico)"
            };
            cmd.Parameters.AddWithValue("@date", newScore.DateToString);
            cmd.Parameters.AddWithValue("@source", newScore.Source);
            cmd.Parameters.AddWithValue("@score", newScore.Score);
            cmd.Parameters.AddWithValue("@provider", newScore.ProviderToString);
            cmd.Parameters.AddWithValue("@fico", Int32Helper.Parse(newScore.FICO));

            return SQLiteHelper.ExecuteCommand(_con, cmd);
        }

        /// <summary>Deletes a <see cref="CreditScore"/> from the database</summary>
        /// <param name="deleteScore"><see cref="CreditScore"/> to be deleted</param>
        /// <returns>True if successful</returns>
        public Task<bool> DeleteCreditScore(CreditScore deleteScore)
        {
            SQLiteCommand cmd = new SQLiteCommand
            {
                CommandText =
                    "DELETE FROM CreditScores WHERE [Date] = @date AND [Source] = @source AND [Score] = @score AND [Provider] = @provider AND [FICO] = @fico"
            };
            cmd.Parameters.AddWithValue("@date", deleteScore.DateToString);
            cmd.Parameters.AddWithValue("@source", deleteScore.Source);
            cmd.Parameters.AddWithValue("@score", deleteScore.Score);
            cmd.Parameters.AddWithValue("@provider", deleteScore.ProviderToString);
            cmd.Parameters.AddWithValue("@fico", Int32Helper.Parse(deleteScore.FICO));

            return SQLiteHelper.ExecuteCommand(_con, cmd);
        }

        /// <summary>Modifies a <see cref="CreditScore"/> in the database.</summary>
        /// <param name="oldScore">Original <see cref="CreditScore"/></param>
        /// <param name="newScore">Modified <see cref="CreditScore"/></param>
        /// <returns>True if successful</returns>
        public Task<bool> ModifyCreditScore(CreditScore oldScore, CreditScore newScore)
        {
            SQLiteCommand cmd = new SQLiteCommand
            {
                CommandText =
                    "UPDATE CreditScores SET [Date] = @date, [Source] = @source, [Score] = @score, [Provider] = @provider, [FICO] = @fico WHERE [Date] = @oldDate AND [Source] = @oldSource AND [Score] = @oldScore AND [Provider] = @oldProvider AND [FICO] = @oldFico"
            };
            cmd.Parameters.AddWithValue("@date", newScore.DateToString);
            cmd.Parameters.AddWithValue("@source", newScore.Source);
            cmd.Parameters.AddWithValue("@score", newScore.Score);
            cmd.Parameters.AddWithValue("@provider", newScore.ProviderToString);
            cmd.Parameters.AddWithValue("@fico", Int32Helper.Parse(newScore.FICO));
            cmd.Parameters.AddWithValue("@oldDate", oldScore.DateToString);
            cmd.Parameters.AddWithValue("@oldSource", oldScore.Source);
            cmd.Parameters.AddWithValue("@oldScore", oldScore.Score);
            cmd.Parameters.AddWithValue("@oldProvider", oldScore.ProviderToString);
            cmd.Parameters.AddWithValue("@oldFico", Int32Helper.Parse(oldScore.FICO));

            return SQLiteHelper.ExecuteCommand(_con, cmd);
        }

        #endregion Credit Score Management

        #region Financial Transaction Management

        /// <summary>Adds a <see cref="FinancialTransaction"/> to an account and the database</summary>
        /// <param name="transaction"><see cref="FinancialTransaction"/> to be added</param>
        /// <param name="account"><see cref="Account"/> the <see cref="FinancialTransaction"/> will be added to</param>
        /// <returns>Returns true if successful</returns>
        public Task<bool> AddFinancialTransaction(FinancialTransaction transaction, Account account)
        {
            SQLiteCommand cmd = new SQLiteCommand
            {
                CommandText =
                    "INSERT INTO FinancialTransactions([Date], [Payee], [MajorCategory], [MinorCategory], [Memo], [Outflow], [Inflow], [Account]) VALUES(@date, @payee, @majorCategory, @minorCategory, @memo, @outflow, @inflow, @name); UPDATE Accounts SET [Balance] = @balance WHERE [Name] = @name"
            };
            cmd.Parameters.AddWithValue("@date", transaction.DateToString);
            cmd.Parameters.AddWithValue("@payee", transaction.Payee);
            cmd.Parameters.AddWithValue("@majorCategory", transaction.MajorCategory);
            cmd.Parameters.AddWithValue("@minorCategory", transaction.MinorCategory);
            cmd.Parameters.AddWithValue("@memo", transaction.Memo);
            cmd.Parameters.AddWithValue("@outflow", transaction.Outflow);
            cmd.Parameters.AddWithValue("@inflow", transaction.Inflow);
            cmd.Parameters.AddWithValue("@name", transaction.Account);
            cmd.Parameters.AddWithValue("@balance", account.Balance);

            return SQLiteHelper.ExecuteCommand(_con, cmd);
        }

        /// <summary>Deletes a <see cref="FinancialTransaction"/> from the database.</summary>
        /// <param name="transaction"><see cref="FinancialTransaction"/> to be deleted</param>
        /// <param name="account"><see cref="Account"/> the <see cref="FinancialTransaction"/> will be deleted from</param>
        /// <returns>Returns true if successful</returns>
        public Task<bool> DeleteFinancialTransaction(FinancialTransaction transaction, Account account)
        {
            SQLiteCommand cmd = new SQLiteCommand
            {
                CommandText =
                    "DELETE FROM FinancialTransactions WHERE [Date] = @date AND [Payee] = @payee AND [MajorCategory] = @majorCategory AND [MinorCategory] = @minorCategory AND [Memo] = @memo AND [Outflow] = @outflow AND [Inflow] = @inflow AND [Account] = @account"
            };
            cmd.Parameters.AddWithValue("@date", transaction.DateToString);
            cmd.Parameters.AddWithValue("@payee", transaction.Payee);
            cmd.Parameters.AddWithValue("@majorCategory", transaction.MajorCategory);
            cmd.Parameters.AddWithValue("@minorCategory", transaction.MinorCategory);
            cmd.Parameters.AddWithValue("@memo", transaction.Memo);
            cmd.Parameters.AddWithValue("@outflow", transaction.Outflow);
            cmd.Parameters.AddWithValue("@inflow", transaction.Inflow);
            cmd.Parameters.AddWithValue("@account", account.Name);

            return SQLiteHelper.ExecuteCommand(_con, cmd);
        }

        /// <summary>Gets the next <see cref="FinancialTransaction"/> ID autoincrement value in the database for the <see cref="FinancialTransaction"/>s table.</summary>
        /// <returns>Next <see cref="FinancialTransaction"/>s ID value</returns>
        public async Task<int> GetNextFinancialTransactionIndex()
        {
            DataSet ds = await SQLiteHelper.FillDataSet(_con, "SELECT * FROM SQLITE_SEQUENCE WHERE name = 'FinancialTransactions'");

            return ds.Tables[0].Rows.Count > 0 ? Int32Helper.Parse(ds.Tables[0].Rows[0]["seq"]) + 1 : 1;
        }

        /// <summary>Modifies the selected <see cref="FinancialTransaction"/> in the database.</summary>
        /// <param name="newTransaction">Transaction to replace the current one in the database</param>
        /// <param name="oldTransaction">Current <see cref="FinancialTransaction"/> in the database</param>
        /// <returns>Returns true if successful</returns>
        public Task<bool> ModifyFinancialTransaction(FinancialTransaction newTransaction, FinancialTransaction oldTransaction)
        {
            SQLiteCommand cmd = new SQLiteCommand
            {
                CommandText = "UPDATE FinancialTransactions SET [Date] = @date, [Payee] = @payee, [MajorCategory] = @majorCategory, [MinorCategory] = @minorCategory, [Memo] = @memo, [Outflow] = @outflow, [Inflow] = @inflow, [Account] = @account WHERE [Date] = @oldDate AND [Payee] = @oldPayee AND [MajorCategory] = @oldMajorCategory AND [MinorCategory] = @oldMinorCategory AND [Memo] = @oldMemo AND [Outflow] = @oldOutflow AND [Inflow] = @oldInflow AND [Account] = @oldAccount"
            };
            cmd.Parameters.AddWithValue("@date", newTransaction.DateToString);
            cmd.Parameters.AddWithValue("@payee", newTransaction.Payee);
            cmd.Parameters.AddWithValue("@majorCategory", newTransaction.MajorCategory);
            cmd.Parameters.AddWithValue("@minorCategory", newTransaction.MinorCategory);
            cmd.Parameters.AddWithValue("@memo", newTransaction.Memo);
            cmd.Parameters.AddWithValue("@outflow", newTransaction.Outflow);
            cmd.Parameters.AddWithValue("@inflow", newTransaction.Inflow);
            cmd.Parameters.AddWithValue("@account", newTransaction.Account);

            cmd.Parameters.AddWithValue("@oldDate", oldTransaction.DateToString);
            cmd.Parameters.AddWithValue("@oldPayee", oldTransaction.Payee);
            cmd.Parameters.AddWithValue("@oldMajorCategory", oldTransaction.MajorCategory);
            cmd.Parameters.AddWithValue("@oldMinorCategory", oldTransaction.MinorCategory);
            cmd.Parameters.AddWithValue("@oldMemo", oldTransaction.Memo);
            cmd.Parameters.AddWithValue("@oldOutflow", oldTransaction.Outflow);
            cmd.Parameters.AddWithValue("@oldInflow", oldTransaction.Inflow);
            cmd.Parameters.AddWithValue("@oldAccount", oldTransaction.Account);

            return SQLiteHelper.ExecuteCommand(_con, cmd);
        }

        #endregion Financial Transaction Management

        #endregion Finances
    }
}
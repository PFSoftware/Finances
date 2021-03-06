﻿using PFSoftware.Extensions;
using System;

namespace PFSoftware.Finances.Models.Data
{
    /// <summary>Represents a monetary transaction in an account.</summary>
    public class FinancialTransaction : BaseINPC
    {
        private int _id;
        private DateTime _date;
        private string _payee, _majorCategory, _minorCategory, _memo, _account;
        private decimal _outflow, _inflow;

        #region Modifying Properties

        public int ID
        {
            get => _id;
            private set { _id = value; NotifyPropertyChanged(nameof(ID)); }
        }

        /// <summary>Date the transaction occurred</summary>
        public DateTime Date
        {
            get => _date;
            private set { _date = value; NotifyPropertyChanged(nameof(Date)); }
        }

        /// <summary>The entity the transaction revolves around</summary>
        public string Payee
        {
            get => _payee;
            private set { _payee = value; NotifyPropertyChanged(nameof(Payee)); }
        }

        /// <summary>Primary category of which the transaction regards</summary>
        public string MajorCategory
        {
            get => _majorCategory;
            set { _majorCategory = value; NotifyPropertyChanged(nameof(MajorCategory)); }
        }

        /// <summary>Secondary category of which the transaction regards</summary>
        public string MinorCategory
        {
            get => _minorCategory;
            set { _minorCategory = value; NotifyPropertyChanged(nameof(MinorCategory)); }
        }

        /// <summary>Extra information regarding the Transaction</summary>
        public string Memo
        {
            get => _memo;
            private set { _memo = value; NotifyPropertyChanged(nameof(Memo)); }
        }

        /// <summary>How much money left the account during Transaction</summary>
        public decimal Outflow
        {
            get => _outflow;
            private set { _outflow = value; NotifyPropertyChanged(nameof(Outflow), nameof(OutflowToString)); }
        }

        /// <summary>How much money entered the account during Transaction</summary>
        public decimal Inflow
        {
            get => _inflow;
            private set { _inflow = value; NotifyPropertyChanged(nameof(Inflow), nameof(InflowToString)); }
        }

        /// <summary>Name of the account Transaction is associated with</summary>
        public string Account
        {
            get => _account;
            set { _account = value; NotifyPropertyChanged(nameof(Account)); }
        }

        #endregion Modifying Properties

        #region Helper Properties

        /// <summary>Date the transaction occurred, formatted properly</summary>
        public string DateToString => Date.ToString("yyyy/MM/dd");

        /// <summary>How much money entered the account during Transaction, formatted to currency</summary>
        public string InflowToString => Inflow.ToString("C2");

        /// <summary>How much money left the account during Transaction, formatted to currency</summary>
        public string OutflowToString => Outflow.ToString("C2");

        #endregion Helper Properties

        #region Override Operators

        private static bool Equals(FinancialTransaction left, FinancialTransaction right)
        {
            if (left is null && right is null) return true;
            if (left is null ^ right is null) return false;
            return left.ID == right.ID && DateTime.Equals(left.Date, right.Date)
                   && string.Equals(left.Payee, right.Payee, StringComparison.OrdinalIgnoreCase)
                   && string.Equals(left.MajorCategory, right.MajorCategory, StringComparison.OrdinalIgnoreCase)
                   && string.Equals(left.MinorCategory, right.MinorCategory, StringComparison.OrdinalIgnoreCase)
                   && string.Equals(left.Memo, right.Memo, StringComparison.OrdinalIgnoreCase)
                   && left.Outflow == right.Outflow && left.Inflow == right.Inflow
                   && string.Equals(left.Account, right.Account, StringComparison.OrdinalIgnoreCase);
        }

        public sealed override bool Equals(object obj) => Equals(this, obj as FinancialTransaction);

        public bool Equals(FinancialTransaction otherTransaction) => Equals(this, otherTransaction);

        public static bool operator ==(FinancialTransaction left, FinancialTransaction right) => Equals(left, right);

        public static bool operator !=(FinancialTransaction left, FinancialTransaction right) => !Equals(left, right);

        public sealed override int GetHashCode() => base.GetHashCode() ^ 17;

        public sealed override string ToString() => string.Join(" - ", DateToString, Account, Payee);

        #endregion Override Operators

        #region Constructors

        /// <summary>Initializes a default instance of Transaction.</summary>
        public FinancialTransaction()
        {
        }

        /// <summary>Initializes an instance of Transaction by assigning Properties.</summary>
        /// <param name="id">ID of Transaction</param>
        /// <param name="date">Date the transaction occurred</param>
        /// <param name="payee">The entity the transaction revolves around</param>
        /// <param name="majorCategory">Primary category of which the transaction regards</param>
        /// <param name="minorCategory">Secondary category of which the transaction regards</param>
        /// <param name="memo">Extra information regarding the transaction</param>
        /// <param name="outflow">How much money left the account during Transaction</param>
        /// <param name="inflow">How much money entered the account during Transaction</param>
        /// <param name="account">Account name related to the transaction</param>
        public FinancialTransaction(int id, DateTime date, string payee, string majorCategory, string minorCategory, string memo, decimal outflow, decimal inflow, string account)
        {
            ID = id;
            Date = date;
            Payee = payee;
            MajorCategory = majorCategory;
            MinorCategory = minorCategory;
            Memo = memo;
            Outflow = outflow;
            Inflow = inflow;
            Account = account;
        }

        /// <summary>Replaces this instance of Transaction with another instance</summary>
        /// <param name="other">Transaction to replace this instance</param>
        public FinancialTransaction(FinancialTransaction other) : this(other.ID, other.Date, other.Payee, other.MajorCategory, other.MinorCategory, other.Memo, other.Outflow, other.Inflow, other.Account)
        {
        }

        #endregion Constructors
    }
}
using LNF.Data;
using System;
using System.Collections.Generic;

namespace FinOps.Models
{
    public class AccountSubsidyModel : ModelBase
    {
        public int AccountSubsidyID { get; set; }
        public DateTime EnableDate { get; set; }
        public int AccountID { get; set; }
        public decimal UserPaymentPercentage { get; set; }
        public IEnumerable<IAccount> Accounts { get; set; }
        public IEnumerable<AccountSubsidyModel> AccountSubsidies { get; set; }
    }
}
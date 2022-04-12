using LNF.Data;
using LNF.Impl.Repository.Billing;
using System;
using System.Collections.Generic;

namespace FinOps.Models
{
    public class MiscChargesModel : ModelBase
    {
        public DateTime Period { get; set; }
        public int ClientID { get; set; }
        public int AccountID { get; set; }
        public string SUBType { get; set; }
        public DateTime ApplyPeriod { get; set; }
        public int Quantity { get; set; }
        public double UnitCost { get; set; }
        public string Description { get; set; }
        public IEnumerable<IClient> Clients { get; set; }
        public IEnumerable<MiscBillingCharge> MiscBillingCharges { get; set; }
    }
}
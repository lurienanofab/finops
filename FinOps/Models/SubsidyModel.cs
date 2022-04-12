using LNF.Repository;
using System;
using System.Collections.Generic;
using System.Data;

namespace FinOps.Models
{
    public class SubsidyModel : ModelBase
    {
        public DateTime Period { get; set; }
        public int ClientID { get; set; }

        public IEnumerable<SubsidyItem> GetItems()
        {
            var dtSubsidy = DataCommand.Create(CommandType.Text)
                .Param("Period", Period)
                .FillDataTable("SELECT tsb.*, c.LName, c.FName FROM sselData.dbo.TieredSubsidyBilling tsb INNER JOIN sselData.dbo.Client c ON c.ClientID = tsb.ClientID WHERE tsb.[Period] = @Period");

            var dtToolBilling = DataCommand.Create(CommandType.Text)
                .Param("Period", Period)
                .FillDataTable("SELECT * FROM sselData.dbo.ToolBilling WHERE [Period] = @Period");

            var dtRoomBilling = DataCommand.Create(CommandType.Text)
                .Param("Period", Period)
                .FillDataTable("SELECT * FROM sselData.dbo.RoomApportionmentInDaysMonthly WHERE [Period] = @Period");

            var dtMiscBilling = DataCommand.Create(CommandType.Text)
                .Param("Period", Period)
                .FillDataTable("SELECT * FROM sselData.dbo.MiscBillingCharge WHERE [Period] = @Period");

            var result = new List<SubsidyItem>();

            foreach (DataRow dr in dtSubsidy.Rows)
            {
                result.Add(new SubsidyItem
                {
                    Period = dr.Field<DateTime>("Period"),
                    ClientID = dr.Field<int>("ClientID"),
                    LName = dr.Field<string>("LName"),
                    FName = dr.Field<string>("FName"),
                    SubsidyDiscount = dr.Field<decimal>("UserTotalSum") - dr.Field<decimal>("UserPaymentSum")
                });
            }

            return result;
        }
    }

    public class SubsidyItem
    {
        public DateTime Period { get; set; }
        public int ClientID { get; set; }
        public string LName { get; set; }
        public string FName { get; set; }
        public decimal SubsidyDiscount { get; set; }
    }
}
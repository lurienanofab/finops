using LNF.Billing;
using System;
using System.Collections.Generic;

namespace FinOps.Models
{
    public class ExternalInvoiceModel : ModelBase
    {
        public DateTime Period { get; set; }
        public bool IncludeRemoteProcessing { get; set; }
        //public IEnumerable<InvoiceModel> Invoices { get; set; }
        public IEnumerable<ExternalInvoice> Invoices { get; set; }
    }
}
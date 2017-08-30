using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Threading.Tasks;

namespace FinOps.Models
{
    public class RemoteProcessingModel : ModelBase
    {
        public DateTime Period { get; set; }
        public int ClientID { get; set; }
        public int RemoteClientID { get; set; }
        public int RemoteAccountID { get; set; }
    }
}
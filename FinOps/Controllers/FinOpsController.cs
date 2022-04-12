using LNF;
using LNF.Data;
using LNF.DataAccess;
using LNF.Web;
using System.Web.Mvc;

namespace FinOps.Controllers
{
    public abstract class FinOpsController : Controller
    {
        public IProvider Provider { get; }

        public FinOpsController(IProvider provider)
        {
            Provider = provider;
        }

        public IClient CurrentUser => HttpContext.CurrentUser(Provider);

        public ISession DataSession => Provider.DataAccess.Session;
    }
}
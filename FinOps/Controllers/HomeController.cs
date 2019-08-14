using FinOps.Models;
using LNF;
using LNF.Cache;
using System.Web.Mvc;
using LNF.Web;

namespace FinOps.Controllers
{
    public class HomeController : Controller
    {
        [Route("")]
        public ActionResult Index(HomeModel model)
        {
            model.CurrentUser = HttpContext.CurrentUser();
            return View(model);
        }

        [Route("return")]
        public ActionResult Return()
        {
            // The session variable is set by LNF.Web.ReturnToAttribute if ReturnTo is in the querystring.

            if (Session["ReturnTo"] == null)
                return RedirectToAction("Index");
            else
            {
                string returnTo = Session["ReturnTo"].ToString();
                Session.Remove("ReturnTo"); 
                return Redirect(returnTo);
            }
        }

        [Route("exit")]
        public ActionResult ExitApplication()
        {
            if (ServiceProvider.Current.IsProduction())
                return Redirect("http://ssel-sched.eecs.umich.edu/sselonline/Blank.aspx");
            else
                return Redirect("/sselonline/Blank.aspx");
        }
    }
}
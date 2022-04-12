using LNF.Impl.DependencyInjection;
using LNF.Web;
using Microsoft.Owin;
using Owin;
using System.Reflection;
using System.Web.Mvc;
using System.Web.Routing;

[assembly: OwinStartup(typeof(FinOps.Startup))]

namespace FinOps
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            var webapp = new WebApp();

            var wcc = webapp.GetConfiguration();
            wcc.RegisterAllTypes();

            webapp.BootstrapMvc(new[] { Assembly.GetExecutingAssembly() });

            app.UseDataAccess(webapp.Context);

            GlobalFilters.Filters.Add(new ReturnToAttribute());
            AreaRegistration.RegisterAllAreas();
            RouteConfig.RegisterRoutes(RouteTable.Routes);
        }
    }
}
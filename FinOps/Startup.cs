using LNF;
using LNF.Impl.DependencyInjection.Web;
using LNF.Web;
using Microsoft.Owin;
using Owin;
using System.Web.Mvc;
using System.Web.Routing;

[assembly: OwinStartup(typeof(FinOps.Startup))]

namespace FinOps
{
    public class Startup : OwinStartup
    {
        public override void Configuration(IAppBuilder app)
        {
            ServiceProvider.Current = IOC.Resolver.GetInstance<ServiceProvider>();
            GlobalFilters.Filters.Add(new ReturnToAttribute());
            base.Configuration(app);
        }

        public override void ConfigureRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");
            routes.MapMvcAttributeRoutes();
        }
    }
}
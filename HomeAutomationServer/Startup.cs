using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(HomeAutomationServer.Startup))]
namespace HomeAutomationServer
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}

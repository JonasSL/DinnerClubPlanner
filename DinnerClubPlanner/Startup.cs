using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(DinnerClubPlanner.Startup))]
namespace DinnerClubPlanner
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}

using System.IO;
using Elsa.Providers.Workflows;
using Elsa.Samples.DocumentApprovalv2.Workflows;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Elsa.Persistence.EntityFramework.Core.Extensions;
using Elsa.Persistence.EntityFramework.PostgreSql;
using Storage.Net;
using Elsa.Extensions;
using Quartz;
using Hangfire;
using Hangfire.PostgreSql;

namespace Elsa.Samples.DocumentApprovalv2
{
    public class Startup
    {
        private readonly IHostEnvironment _environment;

        public Startup(IConfiguration configuration, IHostEnvironment environment)
        {
            _environment = environment;
            Configuration = configuration;
        }
        
        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services
                .AddControllers();

            services
                .AddEndpointsApiExplorer()
                .AddSwaggerGen()
                .AddRedis("localhost:6379,syncTimeout=5000,allowAdmin=True,connectTimeout=500,connectRetry=10")
                    .AddElsa(options => options
                                .UseEntityFrameworkPersistence(ef => ef.UsePostgreSql("Server=localhost;Port=5432;Database=elsa;User Id=postgres;Password=Qwerty123;"), autoRunMigrations: true)
                                .AddConsoleActivities()
                                .UseRedisCacheSignal()
                                .AddEmailActivities(a => {
                                    a.Host = "localhost";
                                    a.Port = 2525;
                                    a.DefaultSender = "noreply@local.host";
                                })
                                .AddHangfireTemporalActivities(hangfire => hangfire.UsePostgreSqlStorage("Server=localhost;Port=5432;Database=elsa;User Id=postgres;Password=Qwerty123;"))
                                .AddWorkflow<DocumentApprovalWorkflow>());

        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseSwagger();
            app.UseSwaggerUI();
            app.UseRouting();
            app.UseEndpoints(endpoints => endpoints.MapControllers());
            app.UseWelcomePage();
        }
    }
}
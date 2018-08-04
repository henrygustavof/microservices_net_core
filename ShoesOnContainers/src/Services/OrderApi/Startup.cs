using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json;
using ShoesOnContainers.Services.OrderApi.Data;
using Microsoft.EntityFrameworkCore;
using System.Reflection;
using MySql.Data.MySqlClient;
using System.Threading;
using Swashbuckle.AspNetCore.Swagger;
using ShoesOnContainers.Services.OrderApi.Infrastructure.Filters;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Autofac;
using MassTransit;
using Autofac.Extensions.DependencyInjection;
using RabbitMQ.Client;
using MassTransit.Util;

namespace ShoesOnContainers.Services.OrderApi
{
    public class Startup
    {
       // private readonly string _connectionString;
        ILogger _logger;

        public IConfiguration Configuration { get; }

        public IContainer ApplicationContainer { get; private set; }
        public Startup(ILoggerFactory loggerFactory,IConfiguration configuration)
        {
            _logger = loggerFactory.CreateLogger<Startup>();
            Configuration = configuration;
            //_connectionString = $@"Server={Environment.GetEnvironmentVariable("MYSQL_SERVICE_NAME")?? "mysqlserver"};
            //                       Port={ "3406" };
            //                       Database={Environment.GetEnvironmentVariable("MYSQL_DATABASE")};
            //                       Uid={Environment.GetEnvironmentVariable("MYSQL_USER")};
            //                       Pwd={Environment.GetEnvironmentVariable("MYSQL_PASSWORD")}";

            //_connectionString = Configuration["ConnectionString"];
        //   _connectionString = "server=mysqlserver;port=3406;database=OrdersDb;Uid=lokum1;pwd=my-secret-pw";
        }

       
       


        // This method gets called by the runtime. Use this method to add services to the container.
        public IServiceProvider ConfigureServices(IServiceCollection services)
        {



            services.AddMvcCore(
                 options => options.Filters.Add(typeof(HttpGlobalExceptionFilter))
                 )
                  .AddJsonFormatters(
                   Options =>
                   {
                       Options.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
                       Options.ContractResolver = new CamelCasePropertyNamesContractResolver();
                   }
                )
                  .AddApiExplorer();
            

            services.Configure<OrderSettings>(Configuration);
          

            ConfigureAuthService(services);
            
            // WaitForDBInit(_connectionString);

            var hostname = Environment.GetEnvironmentVariable("SQLSERVER_HOST") ?? "mssqlserver";
            var password = Environment.GetEnvironmentVariable("SA_PASSWORD") ?? "MyProduct!123";
            var database = Environment.GetEnvironmentVariable("DATABASE") ?? "OrdersDb";

            var connectionString = $"Server={hostname};Database={database};User ID=sa;Password={password};";


            services.AddDbContext<OrdersContext>(options =>
            {
                options.UseSqlServer(connectionString,
                                     sqlServerOptionsAction: sqlOptions =>
                                     {
                                         sqlOptions.MigrationsAssembly(typeof(Startup).GetTypeInfo().Assembly.GetName().Name);
                                         //Configuring Connection Resiliency: https://docs.microsoft.com/en-us/ef/core/miscellaneous/connection-resiliency 
                                         sqlOptions.EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(30), errorNumbersToAdd: null);
                                     });

                // Changing default behavior when client evaluation occurs to throw. 
                // Default in EF Core would be to log a warning when client evaluation is performed.
                options.ConfigureWarnings(warnings => warnings.Throw(RelationalEventId.QueryClientEvaluationWarning));
                //Check Client vs. Server evaluation: https://docs.microsoft.com/en-us/ef/core/querying/client-eval
            });

            

            services.AddSwaggerGen(options =>
            {
                options.DescribeAllEnumsAsStrings();
                options.SwaggerDoc("v1", new Info
                {
                    Title = "Ordering HTTP API",
                    Version = "v1",
                    Description = "The Ordering Service HTTP API",
                    TermsOfService = "Terms Of Service"
                });
                options.AddSecurityDefinition("oauth2", new OAuth2Scheme
                {
                    Type = "oauth2",
                    Flow = "implicit",
                    AuthorizationUrl = $"{Configuration.GetValue<string>("IdentityUrl")}/connect/authorize",
                    TokenUrl = $"{Configuration.GetValue<string>("IdentityUrl")}/connect/token",
                    Scopes = new Dictionary<string, string>()
                    {
                        { "order", "Order Api" }
                    }

                });
                options.OperationFilter<AuthorizeCheckOperationFilter>();
            });
            services.AddCors(options =>
            {
                options.AddPolicy("CorsPolicy",
                    poly => poly.AllowAnyOrigin()
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials());
            });

            var builder = new ContainerBuilder();
            builder.Register(c =>
            {
                return Bus.Factory.CreateUsingRabbitMq(rmq =>
                {
                    rmq.Host(new Uri("rabbitmq://rabbitmq"), "/", h =>
                     {
                         h.Username("guest");
                         h.Password("guest");
                     });
                    rmq.ExchangeType = ExchangeType.Fanout;
                });

            }).
             As<IBusControl>()
            .As<IBus>()
            .As<IPublishEndpoint>()
            .SingleInstance();

            builder.Populate(services);
            ApplicationContainer = builder.Build();
            return new AutofacServiceProvider(ApplicationContainer);

        }
        private void ConfigureAuthService(IServiceCollection services)
        {
            // prevent from mapping "sub" claim to nameidentifier.
            JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

            var identityUrl = Configuration.GetValue<string>("IdentityUrl");

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;

            }).AddJwtBearer(options =>
            {
                options.Authority = identityUrl;
                options.RequireHttpsMetadata = false;
                options.Audience = "order";

            });
        }
        private void WaitForDBInit(string connectionString)
        {
            var connection = new MySqlConnection(connectionString);
            int retries = 1;
            while (retries < 7)
            {
                try
                {
                    Console.WriteLine("Connecting to db. Trial: {0}", retries);
                    connection.Open();
                    connection.Close();
                    break;
                }
                catch (MySqlException)
                {
                    Thread.Sleep((int)Math.Pow(2, retries) * 1000);
                    retries++;
                }
            }
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, IApplicationLifetime lifetime)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            var pathBase = Configuration["PATH_BASE"];
            if (!string.IsNullOrEmpty(pathBase))
            {
                app.UsePathBase(pathBase);
            }
           
         
            app.UseCors("CorsPolicy");
            app.UseAuthentication();
           
            app.UseSwagger()
              .UseSwaggerUI(c =>
              {
                  c.SwaggerEndpoint($"{ (!string.IsNullOrEmpty(pathBase) ? pathBase : string.Empty) }/swagger/v1/swagger.json", "OrderApi V1");
                  c.ConfigureOAuth2("orderswaggerui", "", "", "Ordering Swagger UI");
              });

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller}/{action}/{id?}");
            });

            var bus = ApplicationContainer.Resolve<IBusControl>();
            var busHandle = TaskUtil.Await(() => bus.StartAsync());
            lifetime.ApplicationStopping.Register(() => busHandle.Stop());
        }
    }
}

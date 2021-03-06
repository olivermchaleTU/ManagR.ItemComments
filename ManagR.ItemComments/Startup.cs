using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Threading.Tasks;
using ItemComments.Data;
using ItemComments.Repository;
using ItemComments.Repository.Interfaces;
using ItemComments.Services;
using ItemComments.Services.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ManagR.ItemComments
{
    public class Startup
    {
        public IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940

        
        public void ConfigureServices(IServiceCollection services)
        {
            // Add cors to prevent CORS attacks
            services.AddCors(options =>
            {
                options.AddPolicy("ManagRAppServices",
                builder =>
                {
                    builder.WithOrigins("https://localhost:4200",
                                        "http://localhost:4200")
                                        .AllowAnyOrigin()
                                        .AllowAnyMethod()
                                        .AllowAnyHeader();
                });
            });

            // Add authentication
            JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

            var accountUri = Configuration.GetValue<Uri>("AccountsUrl");
            services.AddAuthentication("Bearer")
            .AddJwtBearer("Bearer", config =>
            {
                config.Authority = accountUri.ToString();
                config.RequireHttpsMetadata = false;
                config.Audience = "ManagR";
            });

            services.AddAuthorization(options =>
            {
                options.AddPolicy("leader", builder =>
                {
                    builder.RequireClaim("role", "analyst", "leader");
                });
                options.AddPolicy("user", builder =>
                {
                    builder.RequireClaim("role", "user", "spectator");
                });
                options.AddPolicy("spectator", builder =>
                {
                    builder.RequireClaim("role", "spectator");
                });
            });

            services.AddControllers();

            // Add db context
            services.AddDbContext<ItemCommentsDb>(options => options.UseSqlServer(
             Configuration.GetConnectionString("ItemComments")));

            services.AddScoped<ICommentsRepository, CommentsRepository>();
            services.AddScoped<ICommenterService, CommenterService>();

            // Create http client available for DI 
            services.AddHttpClient("accounts", c =>
            {
                c.BaseAddress = accountUri;
                c.DefaultRequestHeaders.Accept.Clear();
                c.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
            });



        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseCors("ManagRAppServices");

            app.UseAuthentication();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                // default controller notation
                endpoints.MapDefaultControllerRoute();
            });
        }
    }
}

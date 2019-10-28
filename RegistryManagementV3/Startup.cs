﻿using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RegistryManagementV3.Controllers.Claims;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using RegistryManagementV3.Models;
using RegistryManagementV3.Models.Domain;
using RegistryManagementV3.Models.Repository;
using RegistryManagementV3.Services;

namespace RegistryManagementV3
{
    public class Startup
    {
        private IConfiguration Configuration { get; set; }

        public Startup(IWebHostEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json");
            Configuration = builder.Build();
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton(Configuration);
            services.AddDbContext<SecurityDbContext>(options =>
                options.UseSqlServer(Configuration.GetConnectionString("DefaultConnection")));

            var serviceProvider = services.BuildServiceProvider();
            var unitOfWork = new UnitOfWork(serviceProvider.GetRequiredService<SecurityDbContext>());
            services.AddScoped<IUnitOfWork>(provider => unitOfWork);
            services.AddScoped<ICatalogService>(provider => new CatalogService(unitOfWork));
            var tagService = new TagService(unitOfWork);
            services.AddScoped<ITagService>(provider => tagService);
            services.AddScoped<IResourceService>(provider => new ResourceService(unitOfWork, tagService));
            services.AddScoped<ISearchService>(provider => new SearchService(unitOfWork));
            services.AddScoped<IUserGroupService>(provider => new UserGroupService(unitOfWork));
            services.AddScoped<IUserService>(provider => new UserService(unitOfWork));
            
            services.AddIdentity<ApplicationUser, IdentityRole>()
                .AddEntityFrameworkStores<SecurityDbContext>()
                .AddDefaultTokenProviders();

            services
                .AddScoped<IUserClaimsPrincipalFactory<ApplicationUser>, RegistryManagementClaimsPrincipalFactory>();

//            services.AddIdentity<ApplicationUser, IdentityRole>()
//                .AddEntityFrameworkStores<SecurityDbContext>()
//                .AddDefaultTokenProviders();
            services.Configure<IdentityOptions>(options =>
            {
                options.Password.RequireDigit = false;
                options.Password.RequiredLength = 5;
                options.Password.RequireLowercase = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
            });

            services.AddMvc();
            services.AddRouting();
            services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options => //CookieAuthenticationOptions
                {
                    options.LoginPath = new PathString("/Account/Login");
                });
            services.AddAuthorization(options =>
            {
                options.AddPolicy("onlyUsersWithApprovedAccountPolicy",
                    x => { x.RequireClaim("accountStatus", AccountStatus.Approved.ToString()); });
            });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IServiceProvider serviceProvider)
        {
            CreateRoles(serviceProvider).Wait();
            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(
                    Path.Combine(Directory.GetCurrentDirectory(), "Static")),
                RequestPath = "/Static"
            });

            app.UseAuthentication();
            app.UseAuthorization();
            app.UseRouting();
            using (var serviceScope = app.ApplicationServices.GetService<IServiceScopeFactory>().CreateScope())
            {
                var context = serviceScope.ServiceProvider.GetRequiredService<SecurityDbContext>();
                context.Database.EnsureCreated();
            }

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute("default", "{controller=Home}/{action=Index}");
            });

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
        }

        private static async Task CreateRoles(IServiceProvider serviceProvider)
        {
            //initializing custom roles 
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            string[] roleNames = {"Admin", "User"};

            foreach (var roleName in roleNames)
            {
                var roleExist = await roleManager.RoleExistsAsync(roleName);
                if (!roleExist)
                {
                    //create the roles and seed them to the database: Question 1
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }
        }
    }
}
﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Framework.Core.Helpers;
using log4net;
using log4net.Repository;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using AspectCore.APM.AspNetCore;
using Framework.Core.IoC;
using Framework.Core.Options;
using Framework.Core.Extensions;
using Framework.Core.DbContextCore.CodeFirst;
using Framework.Core.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using Framework.WebApi.Filters;

namespace MySites.Website
{
    public class Startup
    {
        public static ILoggerRepository Repository { get; set; }
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;

            //初始化log4net
            Repository = LogManager.CreateRepository("MySiteRepository");
            Log4NetHelper.SetConfig(Repository, "log4net.config");
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            InitIoC(services);

            services.AddMvc(config =>
            {
                var policy = new AuthorizationPolicyBuilder()
                                 .RequireAuthenticatedUser()
                                 .Build();
                config.Filters.Add(new ApiAuthorizeFilter(policy));
            });
            //services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            //       .AddCookie(o => o.LoginPath = new PathString("/Login"));
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseMvc();

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            app.UseHttpProfiler();      //启动Http请求监控
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseBrowserLink();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }
            app.UseStaticFiles();
            //app.UseMvc(routes =>
            //{
            //    routes.MapRoute(
            //        name: "default",
            //        template: "{controller=Home}/{action=Index}/{id?}");
            //});
        }

        private IServiceProvider InitIoC(IServiceCollection services)
        {
            //database connectionstring
      

            #region Redis

            var redisConnectionString = Configuration.GetConnectionString("Redis");
            //启用Redis
            services.AddDistributedRedisCache(option =>
            {
                option.Configuration = redisConnectionString;//redis连接字符串
                option.InstanceName = "sample";//Redis实例名称
            });
            //全局设置Redis缓存有效时间为5分钟。
            //services.Configure<DistributedCacheEntryOptions>(option =>
            //    option.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5));

            #endregion

            #region MemoryCache

            //启用MemoryCache
            services.AddMemoryCache();

            #endregion

            #region 配置DbContextOption

            //database connectionstring
            var dbConnectionString = Configuration.GetConnectionString("DataBase");//连接字符串
            //配置DbContextOption
            services.Configure<DbContextOption>(options =>
            {
                options.ConnectionString = dbConnectionString;
                options.ModelAssemblyName = "MySites.DataModels";
            });
            #endregion

            #region Database
            var dataBaseType = Configuration.GetConnectionString("DataBaseType");//数据库类型
            if (dataBaseType == "MsSqlServer")
            {
                services.AddSingleton<IDbContextCore, SqlServerDbContext>();//注入EF上下文
            }
            else if (dataBaseType == "MySql")
            {
                services.AddSingleton<IDbContextCore, MySqlDbContext>();//注入EF上下文
            }
            #endregion

            #region 各种注入

            services.AddSingleton(Configuration)//注入Configuration，ConfigHelper要用
                .AddSingleton<IDbContextCore, SqlServerDbContext>()//注入EF上下文
                .AddScopedAssembly("MySites.IServices", "MySites.Services")//注入服务
                .AddScopedAssembly("MySites.IRepositories", "MySites.Repositories");//注入服务
            services.AddMvc(option =>
            {
                option.Filters.Add(new GlobalExceptionFilter());
            })
                .AddControllersAsServices();

            #endregion

            services.AddOptions();

            return AspectCoreContainer.BuildServiceProvider(services);//接入AspectCore.Injector
        }
    }
}

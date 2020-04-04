using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Senparc.CO2NET;
using Senparc.CO2NET.AspNet;
using Senparc.CO2NET.RegisterServices;
using Senparc.Scf.Core;
using Senparc.Scf.Core.AssembleScan;
using Senparc.Scf.Core.Config;
using Senparc.Scf.Core.Models;
using Senparc.Scf.SMS;
using Senparc.Scf.XscfBase;

namespace Senparc.IntegrationSample
{
    public class Startup
    {
        public Startup(IConfiguration configuration, IWebHostEnvironment env)
        {
            Configuration = configuration;
            Env = env;
        }

        public IConfiguration Configuration { get; }
        public IWebHostEnvironment Env { get; set; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            //�ṩ��վ��Ŀ¼
            if (Env.ContentRootPath != null)
            {
                SiteConfig.ApplicationPath = Env.ContentRootPath;
                SiteConfig.WebRootPath = Env.WebRootPath;
            }

            services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });

            services.AddSenparcGlobalServices(Configuration);

            services.AddRazorPages();

            //֧�� Session
            services.AddSession();
            //������Ľ��б�������
            services.AddSingleton(HtmlEncoder.Create(UnicodeRanges.All));
            //ʹ���ڴ滺��
            services.AddMemoryCache();

            //ע�� Lazy<T>
            services.AddTransient(typeof(Lazy<>));

            services.Configure<SenparcCoreSetting>(Configuration.GetSection("SenparcCoreSetting"));
            services.Configure<SenparcSmsSetting>(Configuration.GetSection("SenparcSmsSetting"));

            //�Զ�����ע��ɨ��
            services.ScanAssamblesForAutoDI();
            //�Ѿ���������г����Զ�ɨ���ί�У�����ִ��ɨ�裨���룩
            AssembleScanHelper.RunScan();
            services.AddHttpContextAccessor();
            //���� Xscf ��չ���棨���룩
            services.StartEngine(Configuration);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env,
            IOptions<SenparcCoreSetting> senparcCoreSetting,
            IOptions<SenparcSetting> senparcSetting)
        {
            var registerService = app
                //ȫ��ע��
                .UseSenparcGlobal(env, senparcSetting.Value, globalRegister =>
                 {
                 });

            //XscfModules�����룩
            Senparc.Scf.XscfBase.Register.UseXscfModules(app, registerService);

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorPages();
            });
        }
    }
}

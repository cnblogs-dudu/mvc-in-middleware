using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MvcInMiddleware.Controllers;

namespace MvcInMiddleware
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllersWithViews().AddNewtonsoftJson();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app = env.IsDevelopment() ? app.UseDeveloperExceptionPage() : app.UseExceptionHandler("/Home/Error");
            app.UseStaticFiles();
            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGet("/", async context =>
                {
                    var routeData = new RouteData();
                    routeData.Values.Add("message", "Hello World!");
                    var actionDesciptor = CreateActionDescriptor<HomeController>(nameof(HomeController.Index), routeData);
                    var actionContext = new ActionContext(context, routeData, actionDesciptor);                    
                    var actionInvokerFactory = app.ApplicationServices.GetRequiredService<IActionInvokerFactory>();
                    var invoker = actionInvokerFactory.CreateInvoker(actionContext);
                    await invoker.InvokeAsync();
                });
            });
        }

        private static ActionDescriptor CreateActionDescriptor<TController>(
            string actionName, RouteData routeData)
        {
            var controllerType = typeof(TController);
            var actionDesciptor = new ControllerActionDescriptor()
            {
                ControllerName = controllerType.Name,
                ActionName = actionName,
                FilterDescriptors = new List<FilterDescriptor>(),
                MethodInfo = typeof(HomeController).GetMethod(actionName, BindingFlags.Public | BindingFlags.Instance),
                ControllerTypeInfo = controllerType.GetTypeInfo(),
                Parameters = new List<ParameterDescriptor>(),
                Properties = new Dictionary<object, object>(),
                BoundProperties = new List<ParameterDescriptor>()                
            };

            if (actionDesciptor.MethodInfo == null)
            {
                throw new ArgumentNullException($"actionDesciptor.MethodInfo for '{actionName}'");
            }

            //For searching View
            routeData.Values.Add("controller", actionDesciptor.ControllerName.Replace("Controller", ""));
            routeData.Values.Add("action", actionDesciptor.ActionName);

            foreach (var routeValue in routeData.Values)
            {
                var parameter = new ParameterDescriptor();
                parameter.Name = routeValue.Key;
                var attributes = new object[]
                {
                    new FromRouteAttribute { Name = parameter.Name },
                };
                parameter.BindingInfo = BindingInfo.GetBindingInfo(attributes);
                parameter.ParameterType = routeValue.Value.GetType();
                actionDesciptor.Parameters.Add(parameter);
            }

            return actionDesciptor;
        }
    }
}


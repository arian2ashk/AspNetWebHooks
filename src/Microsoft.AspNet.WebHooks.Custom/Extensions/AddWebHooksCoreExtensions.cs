using System;
using Microsoft.AspNet.WebHooks.Custom.WebHooks;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNet.WebHooks.Custom.Extensions
{
    public static class AddWebHooksCoreExtensions
    {
        public static void AddWebHooksCore(this IServiceCollection services, Action<IServiceCollection> overrideTypes = null)
        {
            services.AddSingleton<IWebHookSender, DataflowWebHookSender>();
            services.AddScoped<IWebHookUser, WebHookUser>();
            services.AddScoped<IWebHookRegistrationsManager, WebHookRegistrationsManager>();
            services.AddScoped<IWebHookNotificationsManager, WebHookNotificationsManager>();
            services.AddScoped<IWebHookFilterManager, WebHookFilterManager>();
            services.AddScoped<IWebHookFilterProvider, WildcardWebHookFilterProvider>();
            services.AddScoped<IWebHookManager, WebHookManager>();
            services.AddSingleton<IWebHookStore, MemoryWebHookStore>();

            overrideTypes?.Invoke(services);
        }
    }
}

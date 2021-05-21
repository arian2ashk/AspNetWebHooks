using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNet.WebHooks.Custom.Extensions
{
    public static class AddWebHooksApiExtensions
    {
        public static void AddWebHooksApi(this IServiceCollection services, Action<IServiceCollection> overrideTypes = null)
        {
            services.AddScoped<IWebHookIdValidator, DefaultWebHookIdValidator>();
            services.AddScoped<IEnumerable<IWebHookRegistrar>>(c=> new IWebHookRegistrar[0]);

            overrideTypes?.Invoke(services);
        }
    }
}

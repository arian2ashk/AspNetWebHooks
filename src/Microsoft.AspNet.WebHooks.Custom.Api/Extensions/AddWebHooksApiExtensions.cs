using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.DependencyInjection;
using Swashbuckle.AspNetCore.SwaggerGen;

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

        public static void AddCommentsAsDocumentation(this SwaggerGenOptions swaggerGenOptions)
        {
            var xmlFile = $"{typeof(AddWebHooksApiExtensions).Assembly.GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            swaggerGenOptions.IncludeXmlComments(xmlPath);
        }
    }
}

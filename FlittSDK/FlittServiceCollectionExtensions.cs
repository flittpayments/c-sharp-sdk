using System;
using Microsoft.Extensions.DependencyInjection;

namespace FlittSDK
{
    /// <summary>
    /// ASP.NET Core dependency-injection registration helpers.
    /// </summary>
    public static class FlittServiceCollectionExtensions
    {
        public static IServiceCollection AddFlitt(
            this IServiceCollection services,
            Action<FlittClientOptions> configure
        )
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            var defaults = new FlittClientOptions();
            configure(defaults);

            services.AddSingleton(defaults);
            services.AddHttpClient(FlittClientFactory.HttpClientName, client =>
            {
                // FlittClient owns timeout/caller cancellation semantics.
                client.Timeout = System.Threading.Timeout.InfiniteTimeSpan;
            });
            services.AddSingleton<IFlittClientFactory, FlittClientFactory>();
            services.AddSingleton<IFlittClient>(provider =>
                provider.GetRequiredService<IFlittClientFactory>()
                    .CreateClient(provider.GetRequiredService<FlittClientOptions>())
            );

            return services;
        }
    }
}

using Microsoft.AspNet.OData;
using Microsoft.AspNet.OData.Batch;
using Microsoft.AspNet.OData.Extensions;
using Microsoft.AspNet.OData.Routing;
using Microsoft.AspNet.OData.Routing.Conventions;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OData;
using Microsoft.Restier.Core;
using Microsoft.Restier.Publishers.OData.Batch;
using Microsoft.Restier.Publishers.OData.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Restier.Publishers.OData.Routing
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class RoutingExtensions
    {
        private const string UseVerboseErrorsFlagKey = "Microsoft.Restier.UseVerboseErrorsFlag";
        private const string RootContainerKey = "System.Web.OData.RootContainerMappingsKey";

        /// TODO GitHubIssue#51 : Support model lazy loading
        /// <summary>
        /// Maps the API routes to the RestierController.
        /// </summary>
        /// <typeparam name="TApi">The user API.</typeparam>
        /// <param name="config">The <see cref="HttpConfiguration"/> instance.</param>
        /// <param name="routeName">The name of the route.</param>
        /// <param name="routePrefix">The prefix of the route.</param>
        /// <param name="batchHandler">The handler for batch requests.</param>
        /// <returns>The task object containing the resulted <see cref="ODataRoute"/> instance.</returns>
        public static IServiceCollection MapRestierRoute<TApi>(
            this IServiceCollection config,
            RestierBatchHandler batchHandler = null)
            where TApi : ApiBase
        {
            // This will be added a service to callback stored in ApiConfiguration
            // Callback is called by ApiBase.AddApiServices method to add real services.
            ApiBase.AddPublisherServices(
                typeof(TApi),
                services =>
                {
                    services.AddODataServices<TApi>();
                });

            config.AddTransient<IContainerBuilder>(sp => new RestierContainerBuilder(typeof(TApi)));
            config.AddService<ODataBatchHandler>((sp, next) => new RestierBatchHandler(sp));

            return config;
        }

        /// <summary>
        /// Creates the default routing conventions.
        /// </summary>
        /// <param name="config">The <see cref="HttpConfiguration"/> instance.</param>
        /// <param name="routeName">The name of the route.</param>
        /// <returns>The routing conventions created.</returns>
        public static IList<IODataRoutingConvention> CreateRestierRoutingConventions(
            this IRouteBuilder config, string routeName)
        {
            var conventions = ODataRoutingConventions.CreateDefaultWithAttributeRouting(routeName, config);
            var index = 0;
            for (; index < conventions.Count; index++)
            {
                var attributeRouting = conventions[index] as AttributeRoutingConvention;
                if (attributeRouting != null)
                {
                    break;
                }
            }

            conventions.Insert(index + 1, new RestierRoutingConvention());
            return conventions;
        }

        public static ODataRoute MapRestierServiceRoute(this IRouteBuilder builder, string routeName, string routePrefix, Action<IContainerBuilder> configureAction)
        {
            var batchHandler = builder.ServiceProvider.GetService<ODataBatchHandler>();
            batchHandler.ODataRouteName = routeName;

            var perRouteContainer = builder.ServiceProvider.GetRequiredService<IPerRouteContainer>();
            perRouteContainer.BuilderFactory = () => builder.ServiceProvider.GetService<IContainerBuilder>();

            return builder.MapODataServiceRoute(routeName, routeName, cb =>
            {
                builder.CreateRestierRoutingConventions(routeName);
                cb.AddService<ODataBatchHandler>(Microsoft.OData.ServiceLifetime.Singleton, sp => batchHandler);
            });
        }
    }
}

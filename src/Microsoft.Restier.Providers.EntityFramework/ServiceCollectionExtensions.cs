// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Model;
using Microsoft.Restier.Core.Query;
using Microsoft.Restier.Core.Submit;

namespace Microsoft.Restier.Providers.EntityFramework
{
    /// <summary>
    /// Contains extension methods of <see cref="IServiceCollection"/>.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// This method is used to add entity framework providers service into container.
        /// </summary>
        /// <typeparam name="TDbContext">The DbContext type.</typeparam>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        /// <returns>Current <see cref="IServiceCollection"/></returns>
        public static IServiceCollection AddEfProviderServices<TDbContext>(this IServiceCollection services)
            where TDbContext : DbContext
        {
            services.AddScoped<DbContext>(sp =>
            {
                return ActivatorUtilities.CreateInstance<TDbContext>(sp);
            });

            return services
                .AddService<IModelBuilder, ModelProducer>()
                .AddService<IModelMapper>((sp, next) => new ModelMapper(typeof(TDbContext)))
                .AddService<IQueryExpressionSourcer, QueryExpressionSourcer>()
                .AddService<IQueryExecutor, QueryExecutor>()
                .AddService<IQueryExpressionProcessor, QueryExpressionProcessor>()
                .AddService<IChangeSetInitializer, ChangeSetInitializer>()
                .AddService<ISubmitExecutor, SubmitExecutor>();
        }
    }
}

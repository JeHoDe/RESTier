// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OData.Edm;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Model;

namespace Microsoft.Restier.Providers.EntityFramework
{
    /// <summary>
    /// Represents a model producer that uses the
    /// metadata workspace accessible from a DbContext.
    /// </summary>
    internal class ModelProducer : IModelBuilder
    {
        public IModelBuilder InnerModelBuilder { get; set; }

        /// <summary>
        /// This class will not real build a model, but only get entity set name and entity map from data source
        /// Then pass the information to publisher layer to build the model.
        /// </summary>
        /// <param name="context">
        /// The context for processing
        /// </param>
        /// <param name="cancellationToken">
        /// An optional cancellation token.
        /// </param>
        /// <returns>
        /// Always a null model.
        /// </returns>
        public Task<IEdmModel> GetModelAsync(ModelContext context, CancellationToken cancellationToken)
        {
            Ensure.NotNull(context, "context");

            var dbContext = context.GetApiService<DbContext>();
            context.ResourceSetTypeMap = dbContext.GetType().GetProperties()
                .Where(e => e.PropertyType.FindGenericType(typeof(DbSet<>)) != null)
                .ToDictionary(e => e.Name, e => e.PropertyType.GetGenericArguments()[0]);
            context.ResourceTypeKeyPropertiesMap = dbContext.Model.GetEntityTypes().ToDictionary(
                e => e.ClrType,
                e => ((ICollection<PropertyInfo>)
                    e.FindPrimaryKey().Properties.Select(p => e.ClrType.GetProperty(p.Name)).ToList()));

            if (InnerModelBuilder != null)
            {
                return InnerModelBuilder.GetModelAsync(context, cancellationToken);
            }

            return Task.FromResult<IEdmModel>(null);
        }
    }
}

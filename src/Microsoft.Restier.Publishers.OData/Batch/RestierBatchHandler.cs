// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNet.OData.Batch;
using Microsoft.AspNetCore.Http;

namespace Microsoft.Restier.Publishers.OData.Batch
{
    /// <summary>
    /// Default implementation of <see cref="ODataBatchHandler"/> in RESTier.
    /// </summary>
    public class RestierBatchHandler : DefaultODataBatchHandler
    {
        private IServiceProvider _services;

        /// <summary>
        /// Initializes a new instance of the <see cref="RestierBatchHandler" /> class.
        /// </summary>
        /// <param name="httpServer">The HTTP server instance.</param>
        public RestierBatchHandler(IServiceProvider services)
        {
            _services = services;
        }

        /// <summary>
        /// Asynchronously parses the batch requests.
        /// </summary>
        /// <param name="request">The HTTP request that contains the batch requests.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The task object that represents this asynchronous operation.</returns>
        public override async Task<IList<ODataBatchRequestItem>> ParseBatchRequestsAsync(HttpContext context)
        {
            Ensure.NotNull(context, "context");
            return await base.ParseBatchRequestsAsync(context);
        }

        /// <summary>
        /// Creates the <see cref="RestierBatchChangeSetRequestItem"/> instance.
        /// </summary>
        /// <param name="changeSetRequests">The list of changeset requests.</param>
        /// <returns>The created <see cref="RestierBatchChangeSetRequestItem"/> instance.</returns>
        protected virtual RestierBatchChangeSetRequestItem CreateRestierBatchChangeSetRequestItem(
            IList<HttpContext> changeSetRequests)
        {
            return new RestierBatchChangeSetRequestItem(_services, changeSetRequests);
        }
    }
}

// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNet.OData.Batch;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Submit;

namespace Microsoft.Restier.Publishers.OData.Batch
{
    /// <summary>
    /// Represents an API <see cref="ChangeSet"/> request.
    /// </summary>
    public class RestierBatchChangeSetRequestItem : ChangeSetRequestItem
    {
        private IServiceProvider _services;

        /// <summary>
        /// Initializes a new instance of the <see cref="RestierBatchChangeSetRequestItem" /> class.
        /// </summary>
        /// <param name="requests">The request messages.</param>
        public RestierBatchChangeSetRequestItem(IServiceProvider services, IEnumerable<HttpContext> requests)
            : base(requests)
        {
            _services = services;
        }

        /// <summary>
        /// Asynchronously sends the request.
        /// </summary>
        /// <param name="invoker">The invoker.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The task object that contains the batch response.</returns>
        public override async Task<ODataBatchResponseItem> SendRequestAsync(RequestDelegate handler)
        {
            Ensure.NotNull(handler, "handler");
            return await base.SendRequestAsync(handler);
        }

        internal async Task SubmitChangeSet(HttpContext request, ChangeSet changeSet)
        {
            using (var api = _services.GetService<ApiBase>())
            {
                SubmitResult submitResults = await api.SubmitAsync(changeSet);
            }
        }

        private static void DisposeResponses(IEnumerable<HttpResponseMessage> responses)
        {
            foreach (HttpResponseMessage response in responses)
            {
                if (response != null)
                {
                    response.Dispose();
                }
            }
        }

        private void SetChangeSetProperty(RestierChangeSetProperty changeSetProperty)
        {
            foreach (var context in this.Contexts)
            {
                context.SetChangeSet(changeSetProperty);
            }
        }
    }
}

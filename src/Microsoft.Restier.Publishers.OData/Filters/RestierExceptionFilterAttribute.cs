// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.OData;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Submit;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Restier.Publishers.OData
{
    /// <summary>
    /// An ExceptionFilter that is capable of serializing well-known exceptions to the client.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true, AllowMultiple = true)]
    internal sealed class RestierExceptionFilterAttribute : ExceptionFilterAttribute
    {
        private static readonly List<ExceptionHandlerDelegate> Handlers = new List<ExceptionHandlerDelegate>
        {
            HandleChangeSetValidationException,
            HandleCommonException
        };

        private delegate Task<bool> ExceptionHandlerDelegate(ExceptionContext context, bool useVerboseErros);

        /// <summary>
        /// The callback to execute when exception occurs.
        /// </summary>
        /// <param name="actionExecutedContext">The context where the action is executed.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The task object that represents the callback execution.</returns>
        public override async Task OnExceptionAsync(ExceptionContext context)
        {
            foreach (var handler in Handlers)
            {
                var result = await handler.Invoke(context, true);

                if (!result)
                {
                    break;
                }
            }
        }

        private static async Task<bool> HandleChangeSetValidationException(
           ExceptionContext context,
           bool useVerboseErros)
        {
            ChangeSetValidationException validationException = context.Exception as ChangeSetValidationException;
            if (validationException != null)
            {
                var errors = validationException.ValidationResults.Select(r => new ValidationResultDto(r));
                var json = new JArray(errors);

                await json.WriteToAsync(new JsonTextWriter(new StreamWriter(context.HttpContext.Response.Body)));

                return false;
            }

            return true;
        }

        private static async Task<bool> HandleCommonException(
            ExceptionContext context,
            bool useVerboseErros)
        {
            var exception = context.Exception;
            if (exception is AggregateException)
            {
                // In async call, the exception will be wrapped as AggregateException
                exception = exception.InnerException;
            }

            if (exception == null)
            {
                return true;
            }

            HttpStatusCode code = HttpStatusCode.Unused;
            if (exception is ODataException)
            {
                code = HttpStatusCode.BadRequest;
            }
            else if (exception is SecurityException)
            {
                code = HttpStatusCode.Forbidden;
            }
            else if (exception is ResourceNotFoundException)
            {
                code = HttpStatusCode.NotFound;
            }
            else if (exception is PreconditionFailedException)
            {
                code = HttpStatusCode.PreconditionFailed;
            }
            else if (exception is PreconditionRequiredException)
            {
                code = (HttpStatusCode)428;
            }
            else if (context.Exception is NotImplementedException)
            {
                code = HttpStatusCode.NotImplemented;
            }

            if (code != HttpStatusCode.Unused)
            {
                context.HttpContext.Response.StatusCode = (int)code;

                if (useVerboseErros)
                {
                    await new JArray(exception).WriteToAsync(new JsonTextWriter(new StreamWriter(context.HttpContext.Response.Body)));
                }
                else
                {
                    using (var writer = new StreamWriter(context.HttpContext.Response.Body))
                    {
                        writer.Write(exception.Message);
                    }
                }

                return false;
            }

            return true;
        }
    }
}

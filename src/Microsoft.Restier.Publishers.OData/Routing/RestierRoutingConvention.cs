// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using Microsoft.AspNet.OData.Extensions;
using Microsoft.AspNet.OData.Routing.Conventions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OData.Edm;
using Microsoft.OData.UriParser;
using ODataPath = Microsoft.AspNet.OData.Routing.ODataPath;

namespace Microsoft.Restier.Publishers.OData
{
    /// <summary>
    /// The default routing convention implementation.
    /// </summary>
    internal class RestierRoutingConvention : IODataRoutingConvention
    {
        class RestierControllerActionDescriptor : ControllerActionDescriptor
        {
            private static Type ControllerType = typeof(RestierController);

            public RestierControllerActionDescriptor(string methodName)
            {
                this.ControllerName = "Restier";
                this.MethodInfo = ControllerType.GetMethod(methodName);
                this.ControllerTypeInfo = ControllerType.GetTypeInfo();
                this.Parameters = new List<ParameterDescriptor>();
                this.FilterDescriptors = new List<FilterDescriptor>();
                this.BoundProperties = new List<ParameterDescriptor>();
                this.ActionName = methodName;

                foreach (var parameter in MethodInfo.GetParameters())
                {
                    AddParameter(parameter);
                }
            }

            private void AddParameter(ParameterInfo parameter)
            {
                if (parameter.ParameterType != typeof(CancellationToken))
                {
                    Parameters.Add(new ControllerParameterDescriptor()
                    {
                        Name = parameter.Name,
                        ParameterType = parameter.ParameterType,
                        BindingInfo = BindingInfo.GetBindingInfo(parameter.GetCustomAttributes().OfType<object>()),
                        ParameterInfo = parameter
                    });
                }
            }
        }

        private static readonly ControllerActionDescriptor MethodNameOfGet = new RestierControllerActionDescriptor("Get");
        private static readonly ControllerActionDescriptor MethodNameOfPost = new RestierControllerActionDescriptor("Post");
        private static readonly ControllerActionDescriptor MethodNameOfPut = new RestierControllerActionDescriptor("Put");
        private static readonly ControllerActionDescriptor MethodNameOfPatch = new RestierControllerActionDescriptor("Patch");
        private static readonly ControllerActionDescriptor MethodNameOfDelete = new RestierControllerActionDescriptor("Delete");
        private static readonly ControllerActionDescriptor MethodNameOfPostAction = new RestierControllerActionDescriptor("PostAction");

        public IEnumerable<ControllerActionDescriptor> SelectActionNew(RouteContext routeContext)
        {
            IActionDescriptorCollectionProvider actionCollectionProvider =
                routeContext.HttpContext.RequestServices.GetRequiredService<IActionDescriptorCollectionProvider>();

            ODataPath odataPath = routeContext.HttpContext.ODataFeature().Path;
            HttpRequest request = routeContext.HttpContext.Request;

            SelectControllerResult controllerResult = new SelectControllerResult(odataPath.Segments.Last().Identifier, null);

            IEnumerable<ControllerActionDescriptor> actionDescriptors = actionCollectionProvider
                .ActionDescriptors.Items.OfType<ControllerActionDescriptor>()
                .Where(c => c.ControllerName == controllerResult.ControllerName);

            return actionDescriptors.Where(
                c => String.Equals(c.ActionName, routeContext.HttpContext.Request.Method, StringComparison.OrdinalIgnoreCase));
        }

        public IEnumerable<ControllerActionDescriptor> SelectAction(RouteContext routeContext)
        {
            var method = new HttpMethod(routeContext.HttpContext.Request.Method);
            var odataPath = routeContext.HttpContext.Request.ODataFeature().Path;
            var isAction = IsAction(odataPath.Segments.LastOrDefault());

            if (method == HttpMethod.Get && !IsMetadataPath(odataPath) && !isAction)
            {
                yield return MethodNameOfGet;
            }

            if (method == HttpMethod.Post && isAction)
            {
                yield return MethodNameOfPostAction;
            }

            if (method == HttpMethod.Post)
            {
                yield return MethodNameOfPost;
            }

            if (method == HttpMethod.Delete)
            {
                yield return MethodNameOfDelete;
            }

            if (method == HttpMethod.Put)
            {
                yield return MethodNameOfPut;
            }

            if (method == new HttpMethod("PATCH"))
            {
                yield return MethodNameOfPatch;
            }

            yield break;
        }

        private static bool IsMetadataPath(ODataPath odataPath)
        {
            return odataPath.PathTemplate == "~" || odataPath.PathTemplate == "~/$metadata";
        }

        private static bool IsAction(ODataPathSegment lastSegment)
        {
            var operationSeg = lastSegment as OperationSegment;
            if (operationSeg != null)
            {
                var action = operationSeg.Operations.FirstOrDefault() as IEdmAction;
                if (action != null)
                {
                    return true;
                }
            }

            var operationImportSeg = lastSegment as OperationImportSegment;
            if (operationImportSeg != null)
            {
                var actionImport = operationImportSeg.OperationImports.FirstOrDefault() as IEdmActionImport;
                if (actionImport != null)
                {
                    return true;
                }
            }

            return false;
        }
    }
}

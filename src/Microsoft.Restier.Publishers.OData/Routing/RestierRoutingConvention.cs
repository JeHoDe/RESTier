// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Net.Http;
using Microsoft.AspNet.OData.Extensions;
using Microsoft.AspNet.OData.Routing.Conventions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;
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
        private const string RestierControllerName = "Restier";
        private IServiceProvider _services;

        public RestierRoutingConvention(IServiceProvider services)
        {
            _services = services;
        }

        /// <summary>
        /// Selects OData controller based on parsed OData URI
        /// </summary>
        /// <param name="odataPath">Parsed OData URI</param>
        /// <param name="request">Incoming HttpRequest</param>
        /// <returns>Prefix for controller name</returns>
        public string SelectController(ODataPath odataPath, HttpRequest request)
        {
            Ensure.NotNull(odataPath, "odataPath");
            Ensure.NotNull(request, "request");

            if (IsMetadataPath(odataPath))
            {
                return null;
            }

            // If user has defined something like PeopleController for the entity set People,
            // Then whether there is an action in that controller is checked
            // If controller has action for request, will be routed to that controller.
            // Cannot mark EntitySetRoutingConversion has higher priority as there will no way
            // to route to RESTier controller if there is EntitySet controller but no related action.
            if (HasControllerForEntitySetOrSingleton(odataPath, request))
            {
                // Fall back to routing conventions defined by OData Web API.
                return null;
            }

            return RestierControllerName;
        }

        /// <summary>
        /// Selects the appropriate action based on the parsed OData URI.
        /// </summary>
        /// <param name="odataPath">Parsed OData URI</param>
        /// <param name="controllerContext">Context for HttpController</param>
        /// <param name="actionMap">Mapping from action names to HttpActions</param>
        /// <returns>String corresponding to controller action name</returns>
        public IEnumerable<ControllerActionDescriptor> SelectAction(RouteContext routeContext)
        {
            // Get a IActionDescriptorCollectionProvider from the global service provider.
            IActionDescriptorCollectionProvider actionCollectionProvider =
                routeContext.HttpContext.RequestServices.GetRequiredService<IActionDescriptorCollectionProvider>();
            Contract.Assert(actionCollectionProvider != null);

            ODataPath odataPath = routeContext.HttpContext.ODataFeature().Path;
            HttpRequest request = routeContext.HttpContext.Request;

            SelectControllerResult controllerResult = new SelectControllerResult(odataPath.Segments.Last().Identifier, null); 

            IEnumerable<ControllerActionDescriptor> actionDescriptors = actionCollectionProvider
                .ActionDescriptors.Items.OfType<ControllerActionDescriptor>()
                .Where(c => c.ControllerName == controllerResult.ControllerName);

            return actionDescriptors.Where(
                c => String.Equals(c.ActionName, routeContext.HttpContext.Request.Method, StringComparison.OrdinalIgnoreCase));
        }

        private static bool IsMetadataPath(ODataPath odataPath)
        {
            return odataPath.PathTemplate == "~" ||
                odataPath.PathTemplate == "~/$metadata";
        }

        private static bool HasControllerForEntitySetOrSingleton(ODataPath odataPath, HttpRequest request)
        {
            string controllerName = null;

            ODataPathSegment firstSegment = odataPath.Segments.FirstOrDefault();
            if (firstSegment != null)
            {
                var entitySetSegment = firstSegment as EntitySetSegment;
                if (entitySetSegment != null)
                {
                    controllerName = entitySetSegment.EntitySet.Name;
                }
                else
                {
                    var singletonSegment = firstSegment as SingletonSegment;
                    if (singletonSegment != null)
                    {
                        controllerName = singletonSegment.Singleton.Name;
                    }
                }
            }

            if (controllerName != null)
            {
                //var controllers = _services.GetHttpControllerSelector().GetControllerMapping();
                //HttpControllerDescriptor descriptor;
                //if (controllers.TryGetValue(controllerName, out descriptor) && descriptor != null)
                //{
                //    // If there is a controller, check whether there is an action
                //    if (HasSelectableAction(request, descriptor))
                //    {
                //        return true;
                //    }
                //}
            }

            return false;
        }

        //private static bool HasSelectableAction(HttpContext context, ControllerDescriptor descriptor)
        //{
        //    var configuration = request.GetConfiguration();
        //    var actionSelector = configuration.Services.GetActionSelector();

        //    // Empty route as this is must and route data is not used by OData routing conversion
        //    var route = new HttpRoute();
        //    var routeData = new HttpRouteData(route);

        //    var context = new ControllerContext(configuration, routeData, request)
        //    {
        //        ControllerDescriptor = descriptor
        //    };

        //    try
        //    {
        //        var action = actionSelector.SelectAction(context);
        //        if (action != null)
        //        {
        //            return true;
        //        }
        //    }
        //    catch (HttpRequestException)
        //    {
        //        // ignored
        //    }

        //    return false;
        //}

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

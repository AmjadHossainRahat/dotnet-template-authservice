using AuthService.API.Settings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace AuthService.API.Authorization
{
    public class EndpointRolesRequirementHandler : AuthorizationHandler<EndpointRolesRequirement>
    {
        private readonly EndpointRolesSettings _rolesSettings;

        public EndpointRolesRequirementHandler(EndpointRolesSettings rolesSettings)
        {
            _rolesSettings = rolesSettings;
        }

        protected override Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            EndpointRolesRequirement requirement)
        {
            string? controllerName = null;
            string? actionName = null;

            // Try to get ControllerActionDescriptor from context.Resource
            if (context.Resource is Microsoft.AspNetCore.Mvc.Filters.AuthorizationFilterContext mvcContext)
            {
                var descriptor = mvcContext.ActionDescriptor as ControllerActionDescriptor;
                controllerName = descriptor?.ControllerName;
                actionName = descriptor?.ActionName;
            }
            else if (context.Resource is Microsoft.AspNetCore.Http.HttpContext httpContext)
            {
                // Fallback for non-MVC contexts
                var endpoint = httpContext.GetEndpoint();
                var descriptor = endpoint?.Metadata.GetMetadata<ControllerActionDescriptor>();
                controllerName = descriptor?.ControllerName;
                actionName = descriptor?.ActionName;
            }

            if (controllerName == null || actionName == null)
            {
                context.Fail();
                return Task.CompletedTask;
            }

            if (!_rolesSettings.RolesPerEndpoint.TryGetValue($"{controllerName}Controller", out var actionRoles))
            {
                // No roles configured for this controller, fail by default
                context.Fail();
                return Task.CompletedTask;
            }

            if (!actionRoles.TryGetValue(actionName, out var allowedRoles))
            {
                // No roles configured for this action, fail by default
                context.Fail();
                return Task.CompletedTask;
            }

            if (allowedRoles.Any(role => context.User.IsInRole(role)))
            {
                context.Succeed(requirement);
            }
            else
            {
                context.Fail();
            }

            return Task.CompletedTask;
        }
    }
}

using Microsoft.AspNetCore.Authorization;

namespace AuthService.API.Authorization
{
    public class EndpointRolesRequirement : IAuthorizationRequirement
    {
        public EndpointRolesRequirement() { }
    }
}

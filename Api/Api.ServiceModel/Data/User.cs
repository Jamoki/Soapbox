using System;
using ServiceStack;
using System.Collections.Generic;
using Rql;
using System.Reflection;
using System.Linq;
using ServiceBelt;

namespace Api.ServiceModel
{
    [Route("/data/users", "GET")]
    [Route("/data/users/{Id}", "GET,DELETE")]
    [LoggedIn(UserRole.Admin)]
    public class UserQuery : ResourceGetParams, IReturn<ListResponse<User>>, IReturn<User>
    {
    }

    public static class UserRole
    {
        public const int Registered = 0;
        public const int User = 1;
        public const int Contributor = 2;
        public const int Admin = 3;
    }

    // NOTE: POST not supported.  To create new user, one MUST use the /register API
    [Route("/data/users/{Id}", "PUT")]
    [LoggedIn(UserRole.Admin)]
    public class User : ResourceBase, IAuthenticatedUser, IReturnVoid, IReturn<PutResponse>, IReturn<PostResponse>
    {
        public string Email { get; set; }
        public string EmailMd5Hash { get; set; }
        public RqlDateTime? EmailValidated { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public int Role { get; set; }
        public List<RqlId> SiteIds { get; set; }
    }
}


using System;
using ServiceStack;
using Rql;

namespace Api.ServiceModel
{
    [Route("/auth/login", "POST")]
    public class RequestLogin : IReturn<RequestLoginResponse>
    {
        public string Email { get; set; }
        public string Password { get; set; }
    }

    public class RequestLoginResponse
    {
        public string LoginToken { get; set; }
        public User User { get; set; }
        public ResponseStatus ResponseStatus { get; set; }
    }

    [Route("/auth/login", "DELETE")]
    // This is the /logout operation.  [LoggedIn("User")] is implied, 
    // but not required.  If not logged in it's a no-op.
    public class Login : IReturn<ErrorResponse>
    {
    }
}


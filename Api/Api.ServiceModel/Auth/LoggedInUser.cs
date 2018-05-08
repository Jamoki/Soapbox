using System;
using ServiceStack;
using System.Collections.Generic;
using Rql;
using ServiceStack.FluentValidation;
using ServiceBelt;

namespace Api.ServiceModel
{
    [Route("/auth/loggedinuser", "GET")]
    [LoggedIn(UserRole.User)]
    public class RequestLoggedInUser : IReturn<User>
    {
    }

    [Route("/auth/loggedinuser", "PUT")]
    [LoggedIn(UserRole.User)]
    public class LoggedInUser : IReturn<PutResponse>
    {
        // ONLY PUT things the user can change about themselves in here!
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string EmailMd5Hash { get; set; }
    }

    public class LoggedInUserValidator : AbstractValidator<LoggedInUser>
    {
        public LoggedInUserValidator()
        {
            RuleFor(x => x.FirstName).NotEmpty();
            RuleFor(x => x.LastName).NotEmpty();
        }
    }
}
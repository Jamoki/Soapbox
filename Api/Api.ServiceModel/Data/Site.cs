using System;
using ServiceStack;
using System.Collections.Generic;
using Rql;
using System.Reflection;
using System.Linq;
using ServiceBelt;

namespace Api.ServiceModel
{
    [Route("/data/sites", "GET")]
    [Route("/data/sites/{Id}", "GET,DELETE")]
    [LoggedIn(UserRole.Admin)]
    public class SiteQuery : ResourceGetParams, IReturn<ListResponse<Site>>, IReturn<Site>
    {
    }

    [Route("/data/sites", "POST")]
    [Route("/data/sites/{Id}", "PUT")]
    [LoggedIn(UserRole.Admin)]
    public class Site : ResourceBase, IReturnVoid, IReturn<PutResponse>, IReturn<PostResponse>
    {
        public string Name { get; set; }
        public string RootPath { get; set; }
    }
}


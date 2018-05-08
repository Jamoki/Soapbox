using System;
using ServiceStack;
using System.Collections.Generic;
using Rql;
using System.Reflection;
using System.Linq;
using ServiceBelt;

namespace Api.ServiceModel
{
    [Route("/data/contents", "GET")]
    [Route("/data/contents/{Id}", "GET,DELETE")]
    [LoggedIn(UserRole.Admin)]
    public class ContentQuery : ResourceGetParams, IReturn<ListResponse<Article>>, IReturn<Article>
    {
    }

    [Route("/data/contents", "POST")]
    [Route("/data/contents/{Id}", "PUT")]
    [LoggedIn(UserRole.Admin)]
    public class Content : ResourceBase, IReturnVoid, IReturn<PutResponse>, IReturn<PostResponse>
    {
        public string MimeType { get; set; }
        public int ByteLength { get; set; }
    }
}


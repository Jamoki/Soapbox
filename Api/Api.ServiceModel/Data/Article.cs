using System;
using ServiceStack;
using System.Collections.Generic;
using Rql;
using System.Reflection;
using System.Linq;
using ServiceBelt;

namespace Api.ServiceModel
{
    [Route("/data/articles", "GET")]
    [Route("/data/articles/{Id}", "GET,DELETE")]
    [LoggedIn(ApplyTo.Delete, UserRole.Admin)]
    public class ArticleQuery : ResourceGetParams, IReturn<ListResponse<Article>>, IReturn<Article>
    {
    }

    [Route("/data/articles", "POST")]
    [Route("/data/articles/{Id}", "PUT")]
    [LoggedIn(UserRole.Admin)]
    public class Article : ResourceBase, IReturnVoid, IReturn<PutResponse>, IReturn<PostResponse>
    {
        public string Title { get; set; }
        public string Summary { get; set; }
        public string ImageUrl { get; set; }
        public RqlId SiteId { get; set; }
        public int Revision { get; set; }
        public List<string> Tags { get; set; }
        public RqlId BodyId { get; set; }
        public RqlId HtmlId { get; set; }
        public List<RqlId> ImageIds { get; set; }
    }
}


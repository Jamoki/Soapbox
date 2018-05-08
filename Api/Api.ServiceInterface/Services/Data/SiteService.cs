using System;
using System.Net;
using Api.ServiceInterface;
using MongoDB.Bson;
using MongoDB.Driver;
using Rql;
using Rql.MongoDB;
using ServiceStack;
using ServiceBelt;
using Dmo = Shared.DataModel;
using Smo = Api.ServiceModel;

namespace Api.ServiceInterface
{
    public class SiteService : MongoService<Smo.Site, Smo.SiteQuery, Dmo.Site>
    {
    }
}


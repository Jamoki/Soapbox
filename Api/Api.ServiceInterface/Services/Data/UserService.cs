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
using System.Threading.Tasks;

namespace Api.ServiceInterface
{
    public class UserService : MongoService<Smo.User, Smo.UserQuery, Dmo.User>
    {
        public ISessionManager Session { get; set; }

        public override void Delete(Smo.UserQuery smoQuery)
        {
            base.Delete(smoQuery);

            Session.LogoutUser(smoQuery.Id.Value);
        }

        public override Task<ServiceBelt.PutResponse> Put(Smo.User smo)
        {
            var obj = base.Put(smo);

            Session.LogoutUser(smo.Id);

            return obj;
        }
    }
}


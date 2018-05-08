using System;
using ServiceStackService = global::ServiceStack.Service;
using Dmo=Shared.DataModel;
using Smo=Api.ServiceModel;
using Rql;
using ServiceBelt;
using Rql.MongoDB;
using ServiceStack;
using System.Linq;
using MongoDB.Driver;
using ServiceStack.FluentValidation;
using System.Threading.Tasks;

namespace Api.ServiceInterface
{
    public class LoggedInUserService : ServiceStackService
    {
        public ISessionManager Session { get; set; }
        public IMongoManager Mongo { get; set; }
        public IValidator<Smo.LoggedInUser> SmoValidator { get; set; }

        public object Get(Smo.RequestLoggedInUser request)
        {
            return Session.GetLoggedInUserAs<Smo.User>(this.Request);
        }

        public async Task<object> Put(Smo.LoggedInUser request)
        {
            SmoValidator.ValidateAndThrow(request);

            var smoUser = Session.GetLoggedInUserAs<Smo.User>(this.Request);
            var id = smoUser.Id.ToObjectId();
            var users = Mongo.GetCollection<Dmo.User>();
            var utcNow = DateTime.UtcNow;
            var filter = Builders<Dmo.User>.Filter.Eq(e => e.Id, id);
            var update = Builders<Dmo.User>.Update
                .Set(e => e.Updated, utcNow)
                .Set(e => e.FirstName, request.FirstName)
                .Set(e => e.LastName, request.LastName);
            var dmoUser = await users.FindOneAndUpdateAsync(filter, update);

            if (dmoUser == null)
                throw HttpError.NotFound("User was not found");

            PropertyCopier.Copy(request, smoUser);
            smoUser.Updated = new RqlDateTime(dmoUser.Updated);

            Session.UpdateLoggedInUser(smoUser);

            return new PutResponse(smoUser.Updated);
        }
    }
}


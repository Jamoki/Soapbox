using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Mail;
using MongoDB.Driver;
using MongoDB.Bson;
using Rql.MongoDB;
using ServiceStack;
using ToolBelt;
using Dmo=Shared.DataModel;
using Smo = Api.ServiceModel;
using ServiceStackService = global::ServiceStack.Service;
using ServiceBelt;
using ServiceStack.Auth;
using System.Linq;
using System.Threading.Tasks;

namespace Api.ServiceInterface
{
    public class LoginService : ServiceStackService
    {
        public IMongoManager Mongo { get; set; }
        public ISessionManager Session { get; set; }

        public async Task<object> Post(Smo.RequestLogin request)
        {
            var dmoUser = await Mongo.GetCollection<Dmo.User>().Find(Builders<Dmo.User>.Filter.Eq(e => e.Email, request.Email)).FirstOrDefaultAsync();

            if (dmoUser == null)
                throw HttpError.Conflict("Invalid user or password");

            // Validate the users password!
            if (!new SaltedHash().VerifyHashString(request.Password, dmoUser.PasswordHash, dmoUser.Salt))
                throw HttpError.Conflict("Invalid user or password");

            // Return a new login token for the session
            var response = new Smo.RequestLoginResponse();

            response.User = new Smo.User();

            PropertyCopier.Copy(dmoUser, response.User);

            response.LoginToken = Session.LoginUser(response.User);

            return response;
        }

        public object Delete(Smo.Login request)
        {
            Session.LogoutUser(this.Request);

            return new ErrorResponse();
        }
    }
}


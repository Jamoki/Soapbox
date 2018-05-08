using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using ServiceStack.DataAnnotations;
using System.Collections.Generic;
using Rql;
using ServiceStack.FluentValidation;
using System.Linq;
using Shared.DataModel;
using ServiceBelt;

namespace Shared.DataModel
{
    public static class UserRole
    {
        public const int Registered = 0;
        public const int User = 1;
        public const int Contributor = 2;
        public const int Admin = 3;
    }

    public class User : ICollectionObject
    {
        public ObjectId Id { get; set; }
        public DateTime Created { get; set; }
        public DateTime Updated { get; set; }
        [Index(unique: true)]
        public string Email { get; set; }
        [BsonIgnoreIfNull]
        public DateTime? EmailValidated { get; set; }
        public string FirstName { get; set; }
        [BsonIgnoreIfNull]
        public string LastName { get; set; }
        [BsonIgnoreIfNull]
        public int Role { get; set; }
        public string Salt { get; set; }
        public string PasswordHash { get; set; }
        [References(typeof(Site))]
        public List<ObjectId> SiteIds { get; set; }
    }

    public class UserValidator : AutowiredValidator<User>
    {
        public IMongoManager Mongo { get; set; }

        protected override void AddRules()
        {
            RuleFor(x => x.Email).NotNull().EmailAddress();
            RuleFor(x => x.FirstName).NotEmpty().Unless(x => x.FirstName == null);
            RuleFor(x => x.LastName).NotEmpty().Unless(x => x.LastName == null);
            RuleFor(x => x.Salt).NotEmpty();
            RuleFor(x => x.PasswordHash).NotEmpty();
            RuleFor(x => x.Created).NotEmpty();
            RuleFor(x => x.Updated).NotEmpty().GreaterThanOrEqualTo(x => x.Created);
        }
    }
}


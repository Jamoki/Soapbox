using System;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Rql;
using ServiceBelt;
using ServiceStack.DataAnnotations;
using ServiceStack.FluentValidation;

namespace Shared.DataModel
{
    public class Content : ICollectionObject
    {
        public ObjectId Id { get; set; }
        public DateTime Created { get; set; }
        public DateTime Updated { get; set; }
        public string MimeType { get; set; }
        public int ByteLength { get; set; }
        [BsonRepresentation(BsonType.Binary)]
        public byte[] Data { get; set; }
    }

    public class ContentValidator : AutowiredValidator<Content>
    {
        protected override void AddRules()
        {
            RuleFor(x => x.ByteLength).NotEmpty();
            RuleFor(x => x.Data).NotNull();
            RuleFor(x => x.MimeType).NotNull();
            RuleFor(x => x.Created).NotEmpty();
            RuleFor(x => x.Updated).NotEmpty().GreaterThanOrEqualTo(x => x.Created);
        }
    }
}


using System;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Rql;
using ServiceBelt;
using ServiceStack.DataAnnotations;
using ServiceStack.FluentValidation;
using Shared.DataModel;

namespace Shared.DataModel
{
    public class Article : ICollectionObject
    {
        public ObjectId Id { get; set; }
        public DateTime Created { get; set; }
        public DateTime Updated { get; set; }
        public string Title { get; set; }
        public string Summary { get; set; }
        public string ImageUrl { get; set; }
        public ObjectId SiteId { get; set; }
        [Index(unique: true)]
        public int Revision { get; set; }
        public List<string> Tags { get; set; }
        [References(typeof(Content))]
        public ObjectId BodyId { get; set; }
        [References(typeof(Content))]
        public ObjectId HtmlId { get; set; }
        [References(typeof(Content))]
        public List<ObjectId> ImageIds { get; set; }
    }

    public class ArticleValidator : AutowiredValidator<Article>
    {
        protected override void AddRules()
        {
            RuleFor(x => x.Revision).GreaterThanOrEqualTo(0);
            RuleFor(x => x.Title).NotEmpty();
            RuleFor(x => x.Summary).NotEmpty();
            RuleFor(x => x.ImageUrl);
            RuleFor(x => x.SiteId).NotEmpty();
            RuleFor(x => x.Tags).NotNull();
            RuleFor(x => x.BodyId).NotEmpty();
            RuleFor(x => x.HtmlId).NotEmpty();
            RuleFor(x => x.ImageIds).NotNull();
            RuleFor(x => x.Created).NotEmpty();
            RuleFor(x => x.Updated).NotEmpty().GreaterThanOrEqualTo(x => x.Created);
        }
    }
}


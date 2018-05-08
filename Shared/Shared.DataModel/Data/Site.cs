using System;
using ServiceBelt;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using ToolBelt;
using ServiceStack.DataAnnotations;
using ServiceStack.FluentValidation;

namespace Shared.DataModel
{
    public class Site : ICollectionObject
    {
        public ObjectId Id { get; set; }
        public DateTime Created { get; set; }
        public DateTime Updated { get; set; }
        public string Name { get; set; }
        public string RootPath { get; set; }

        public void GetPaths(out ParsedPath articlesPath, out ParsedPath contentsPath)
        {
            var envVars = Environment.GetEnvironmentVariables();
            var path = new ParsedDirectoryPath(RootPath.ReplaceTags("$(", ")", envVars, TaggedStringOptions.ThrowOnUnknownTags));

            articlesPath = path.Append("articles", PathType.Directory);
            contentsPath = path.Append("contents", PathType.Directory);
        }
    }

    public class SiteValidator : AutowiredValidator<Site>
    {
        protected override void AddRules()
        {
            RuleFor(x => x.Name).NotEmpty();
            RuleFor(x => x.RootPath).NotNull();
            RuleFor(x => x.Created).NotEmpty();
            RuleFor(x => x.Updated).NotEmpty().GreaterThanOrEqualTo(x => x.Created);
        }
    }
}


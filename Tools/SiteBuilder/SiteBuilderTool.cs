using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.Text;
using ToolBelt;
using Dmo=Shared.DataModel;
using Rql;
using ServiceBelt;
using Api.ServiceInterface;
using Rql.MongoDB;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Soapbox
{
    [CommandLineTitle("Soapbox Article Builder Tool")]
    [CommandLineDescription("Builds the web pages and images for published articles")]
    [CommandLineCopyright("Copyright (c) 2015, Jamoki")]
    public class SiteBuilderTool : ToolBase
    {
        [CommandLineArgument("help", ShortName="?", Description="Shows this help")]
        public bool ShowUsage { get; set; }

        [CommandLineArgument("mongo", ShortName="m", Description="URL of the MongoDB server, e.g. mongo://localhost:27017")]
        [AppSettingsArgument]
        public ParsedUrl MongoDbUrl { get; set; }

        IMongoManager Mongo { get; set; }
        readonly Type dataMarkerType = typeof(Dmo.ISharedDataModel);

        public override void Execute()
        {
            WriteMessage(this.Parser.LogoBanner);

            if (ShowUsage)
            {
                WriteMessage(this.Parser.Usage);
                return;
            }

            if (MongoDbUrl == null)
            {
                WriteError("A MongoDB URL must be specified");
                return;
            }

            this.Mongo = new MongoManager(new MongoDB.Driver.MongoUrl(MongoDbUrl), dataMarkerType);

            var sites = Mongo.GetCollection<Dmo.Site>().Find(new BsonDocument()).ToListAsync().Result;

            // Iterate through all sites
            foreach (var site in sites)
            {
                WriteMessage("Updating site '{0}' - '{1}'", site.Id.ToRqlId(), site.Name);

                ParsedPath articlesPath;
                ParsedPath contentsPath;

                site.GetPaths(out articlesPath, out contentsPath);

                CheckDirectoryExists(articlesPath);
                CheckDirectoryExists(contentsPath);

                // Get list of articles for that site
                var articles = Mongo.GetCollection<Dmo.Article>().Find(a => a.SiteId == site.Id).ToListAsync().Result;

                foreach (var article in articles)
                {
                    var contentColl = Mongo.GetCollection<Dmo.Content>();
                    var html = contentColl.Find(c => c.Id == article.HtmlId).FirstAsync().Result;
                    var images = contentColl.Find(Builders<Dmo.Content>.Filter.In(c => c.Id, article.ImageIds)).ToListAsync().Result;

                    // Update the article and content
                    SitesManager.UpdateArticleFiles(article, html, articlesPath);
                    SitesManager.UpdateContentFiles(images, contentsPath);
                }
            }
        }

        void CheckDirectoryExists(ParsedPath path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }
    }
}


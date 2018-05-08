using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.Text;
using ToolBelt;
using Dmo=Shared.DataModel;
using ServiceBelt;
using ServiceStack.Auth;
using Rql;
using Rql.MongoDB;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Soapbox
{
    [CommandLineTitle("Soapbox Site Tool")]
    [CommandLineDescription("Manages Soapbox Sites")]
    [CommandLineCopyright("Copyright (c) 2015, Jamoki")]
    [CommandLineCommandDescription("new", Description = "Creates a new site")]
    [CommandLineCommandDescription("show", Description = "Show a site")]
    [CommandLineCommandDescription("list", Description = "List all sites")]
    [CommandLineCommandDescription("delete", Description = "Deletes a site")]
    [CommandLineCommandDescription("update", Description = "Updates an existing site")]
    [CommandLineCommandDescription("help", Description = "Displays help for this tool ")]
    public class SiteEditorTool : ToolBase
    {
        [CommandLineArgument("help", ShortName="?", Description="Shows this help", Commands="show,new,delete,update,list")]
        public bool ShowUsage { get; set; }
        [CommandCommandLineArgument(Description="One of new, update, delete, list, show or help", Commands="show,new,delete,update,list,help")]
        public string Command { get; set; }
        [AppSettingsArgument]
        [CommandLineArgument("mongo", ShortName="m", Description="URL of the MongoDB server, default is mongodb://localhost:27017/dividend", Commands="new,delete,show,list,update")]
        public ParsedUrl MongoDbUrl { get; set; }
        [CommandLineArgument("siteid", ShortName="i", Description="Site identifier.  This is the RQL id.", Commands="update,delete,show")]
        public RqlId SiteId { get; set; }
        [CommandLineArgument("rootpath", ShortName="r", Description="Root path for the site.  Can contain environment variables, e.g. $(HOME)", Commands="update,new,delete")]
        public string RootPath { get; set; }
        [CommandLineArgument("name", ShortName="n", Description="Site name", Commands="new,update,delete")]
        public string Name { get; set; }
        [DefaultCommandLineArgument(Description = "Name of a command", Commands="help")]
        public string Default { get; set; }

        private IMongoManager Mongo { get; set; }
        private IHiddenDataManager Secret { get; set; }
        private Dmo.SiteValidator DmoValidator { get; set; }

        public override void Execute()
        {
            WriteMessage(this.Parser.LogoBanner);

            if (ShowUsage || this.Command == "help")
            {
                if (this.Command != "help")
                    WriteMessage(this.Parser.Usage);
                else
                    WriteMessage(this.Parser.GetUsage(Default));

                return;
            }

            if (MongoDbUrl == null)
            {
                WriteError("A MongoDB database URL must be specified");
                return;
            }

            this.Mongo = new MongoManager(new MongoDB.Driver.MongoUrl(MongoDbUrl), typeof(Dmo.ISharedDataModel));
            this.DmoValidator = new Dmo.SiteValidator();

            Task task = null;

            switch (Command)
            {
            case "new":
                task = NewSite();
                break;
            case "update":
                task = UpdateSite();
                break;
            case "delete":
                task = DeleteSite();
                break;
            case "show":
                task = ShowSite();
                break;
            case "list":
                task = ListSites();
                break;
            default:
                WriteError("Unknown command '{0}'", Command);
                break;
            }

            if (task != null)
                task.Wait();
        }

        async Task ShowSite()
        {
            if (SiteId == RqlId.Zero)
            {
                WriteError("Site id must be specified");
                return;
            }

            var sites = Mongo.GetCollection<Dmo.Site>();
            var site = await sites.Find(s => s.Id == SiteId.ToObjectId()).FirstOrDefaultAsync();

            if (site == null)
            {
                WriteError("Site '{0}' does not exist", SiteId);
                return;
            }

            Console.WriteLine("Id: {0}", site.Id.ToRqlId());
            Console.WriteLine("Created: {0}", site.Created);
            Console.WriteLine("Updated: {0}", site.Updated);
            Console.WriteLine("Name: {0}", site.Name);
            Console.WriteLine("Root Path: {0}", site.RootPath);
        }

        async Task ListSites()
        {
            var sites = await Mongo.GetCollection<Dmo.Site>().Find(new BsonDocument()).ToListAsync();

            foreach (var site in sites)
            {
                Console.WriteLine("Id: {0}, Name: '{1}'", site.Id.ToRqlId(), site.Name);
            }
        }

        async Task DeleteSite()
        {
            if (SiteId == RqlId.Zero)
            {
                WriteError("Site id must be specified");
                return;
            }

            var sites = Mongo.GetCollection<Dmo.Site>();

            Console.WriteLine("Do you really want to delete site '{0}'? (y/n): ", SiteId);

            var input = Console.ReadLine();

            if (input != "y")
                return;

            // TODO: Remove related content and articles

            var result = await sites.DeleteOneAsync(s => s.Id == SiteId.ToObjectId());

            if (result.DeletedCount == 1)
            {
                WriteMessage("Site '{0}' removed", SiteId);
            }
        }

        async Task UpdateSite()
        {
            if (SiteId == RqlId.Zero)
            {
                WriteError("Site id must be specified");
                return;
            }

            var sites = Mongo.GetCollection<Dmo.Site>();
            var site = await sites.Find(s => s.Id == SiteId.ToObjectId()).FirstOrDefaultAsync();

            if (site == null)
            {
                WriteError("Site id '{0}' does not exist", SiteId);
                return;
            }

            bool updated = false;

            if (!String.IsNullOrEmpty(RootPath))
            {
                site.RootPath = RootPath;
                updated = true;
            }

            if (!String.IsNullOrEmpty(Name))
            {
                site.Name = Name;
                updated = true;
            }

            if (updated)
            {
                site.Updated = DateTime.UtcNow;

                await sites.ReplaceOneAsync(s => s.Id == site.Id, site);

                WriteMessage("Site '{0}' updated", site.Id.ToRqlId());
                await ShowSite();
            }
            else
            {
                WriteWarning("Nothing updated");
            }
        }

        async Task NewSite()
        {
            if (String.IsNullOrWhiteSpace(Name))
            {
                WriteError("Non-blank site name must be given");
                return;
            }

            var sites = Mongo.GetCollection<Dmo.Site>();

            if (await sites.CountAsync(s => s.Name == Name) != 0)
            {
                WriteError("Site named '{0}' already exists", Name);
                return;
            }

            Dmo.Site site = new Dmo.Site();

            site.Name = Name;
            site.RootPath = RootPath;
            site.Updated = site.Created = DateTime.UtcNow;

            // Validated the user to be safe before we write it
            var result = DmoValidator.Validate(site);

            if (!result.IsValid)
            {
                WriteError(String.Join("\n\t", result.Errors.Select(e => e.AttemptedValue + " for " + e.ErrorMessage)));
                return;
            }

            // Add an entry in the database for the user
            await sites.InsertOneAsync(site);

            Console.WriteLine("Site id '{0}' created", site.Id.ToRqlId());
        }
    }
}


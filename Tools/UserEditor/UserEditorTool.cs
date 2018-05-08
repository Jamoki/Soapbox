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
using MongoDB.Driver;
using MongoDB.Bson;
using System.Threading.Tasks;

namespace Soapbox
{
    [CommandLineTitle("Soapbox User Tool")]
    [CommandLineDescription("Manages Soapbox Users")]
    [CommandLineCopyright("Copyright (c) 2015, Jamoki")]
    [CommandLineCommandDescription("new", Description = "Creates a new user")]
    [CommandLineCommandDescription("list", Description = "Lists all users")]
    [CommandLineCommandDescription("show", Description = "Show a user")]
    [CommandLineCommandDescription("delete", Description = "Deletes a user")]
    [CommandLineCommandDescription("update", Description = "Updates an existing user")]
    [CommandLineCommandDescription("help", Description = "Displays help for this tool ")]
    public class UserEditorTool : ToolBase
    {
        [CommandLineArgument("help", ShortName="?", Description="Shows this help", Commands="show,new,delete,update,list")]
        public bool ShowUsage { get; set; }
        [CommandCommandLineArgument(Description="One of new, delete, update or help", Commands="show,new,delete,update,help")]
        public string Command { get; set; }
        [AppSettingsArgument]
        [CommandLineArgument("mongo", ShortName="m", Description="URL of the MongoDB server, default is mongodb://localhost:27017/dividend", Commands="new,delete,update,list")]
        public ParsedUrl MongoDbUrl { get; set; }
        [CommandLineArgument("email", ShortName="e", Description="Email of the user e.g. joe@brown.org", Commands="show,new,delete,update")]
        public ParsedEmail Email { get; set; }
        [CommandLineArgument("password", ShortName="p", Description="Password for the user", Commands="new,delete,update")]
        public string Password { get; set; }
        [CommandLineArgument("firstname", ShortName="fn", Description="First name", Commands="new,update")]
        public string FirstName { get; set; }
        [CommandLineArgument("lastname", ShortName="ln", Description="Last name", Commands="new,update")]
        public string LastName { get; set; }
        [DefaultCommandLineArgument(Description = "Name of a command", Commands="help")]
        public string Default { get; set; }

        private IMongoManager Mongo { get; set; }
        private IHiddenDataManager Secret { get; set; }
        private Dmo.UserValidator DmoValidator { get; set; }

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
            this.DmoValidator = new Dmo.UserValidator();

            Task task = null;

            switch (Command)
            {
            case "list":
                task = ListUsers();
                break;
            case "new":
                task = NewUser();
                break;
            case "update":
                task = UpdateUser();
                break;
            case "show":
                task = ShowUser();
                break;
            default:
                WriteError("Unknown command {0}", Command);
                break;
            }

            if (task != null)
                task.Wait();
        }

        async Task ListUsers()
        {
            var users = await Mongo.GetCollection<Dmo.User>().Find(new BsonDocument()).ToListAsync();

            foreach (var user in users)
            {
                Console.WriteLine("Id: {0}, First/Last Name: {1} {2}", user.Id.ToRqlId(), user.FirstName, user.LastName);
            }
        }

        async Task ShowUser()
        {
            if (Email == null)
            {
                WriteError("User email must be specified");
                return;
            }

            var users = Mongo.GetCollection<Dmo.User>();
            var user = await users.Find(u => u.Email == Email.UserAndHost).FirstOrDefaultAsync();

            if (user == null)
            {
                WriteError("User '{0}' does not exist", Email);
                return;
            }

                    Console.WriteLine("Id: {0}", user.Id.ToRqlId());
            Console.WriteLine("Email: {0}", user.Email);
            Console.WriteLine("Created: {0}", user.Created);
            Console.WriteLine("Updated: {0}", user.Updated);
            Console.WriteLine("First Name: {0}", user.FirstName);
            Console.WriteLine("Last Name: {0}", user.LastName);
        }

        async Task UpdateUser()
        {
            if (Email == null)
            {
                WriteError("User email must be specified");
                return;
            }

            var users = Mongo.GetCollection<Dmo.User>();
            var user = await users.Find(u => u.Email == Email.UserAndHost).FirstOrDefaultAsync();

            if (user == null)
            {
                WriteError("User '{0}' does not exist", Email);
                return;
            }

            bool updated = false;

            if (!String.IsNullOrEmpty(Password))
            {
                // Hash and salt the users password
                string salt;
                string hash;

                new SaltedHash().GetHashAndSaltString(Password, out hash, out salt);

                user.PasswordHash = hash;
                user.Salt = salt;
                updated = true;
            }

            if (updated)
            {
                user.Updated = DateTime.UtcNow;
                await users.ReplaceOneAsync(u => u.Id == user.Id, user);

                Console.WriteLine("Updated user '{0}'", user.Id.ToRqlId());
            }
        }

        async Task NewUser()
        {
            var users = Mongo.GetCollection<Dmo.User>();

            if (await users.CountAsync(u => u.Email == Email) > 0)
            {
                WriteError("User already exists");
                return;
            }

            Dmo.User user = new Dmo.User();

            user.Email = Email.UserAndHost;
            user.Updated = DateTime.UtcNow;
            user.Created = DateTime.UtcNow;
            user.FirstName = FirstName;
            user.LastName = LastName;
            user.Role = Dmo.UserRole.Admin;

            // Hash and salt the users password
            string salt;
            string hash;

            new SaltedHash().GetHashAndSaltString(Password, out hash, out salt);

            user.PasswordHash = hash;
            user.Salt = salt;

            user.Updated = user.Created = DateTime.UtcNow;
            user.Role = Dmo.UserRole.User;

            // Validated the user to be safe before we write it
            var result = DmoValidator.Validate(user);

            if (!result.IsValid)
            {
                WriteError(String.Join("\n\t", result.Errors.Select(e => e.AttemptedValue + " for " + e.ErrorMessage)));
                return;
            }

            // Add an entry in the database for the user
            await users.InsertOneAsync(user);
        }
    }
}


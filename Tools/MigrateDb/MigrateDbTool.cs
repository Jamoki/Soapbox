using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using MongoDB.Bson;
using MongoDB.Driver;
using Rql;
using ToolBelt;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Dividend
{
    [CommandLineTitle("Soapbox Database Migration Tool")]
    [CommandLineDescription("A tool for migrating the Soapbox database.  A MONGO_URL is of the form mongodb://password:user@host:port/database .")]
    public class MigrateDbTool : ToolBase
    {
        private IMongoDatabase fromDb;
        private IMongoDatabase toDb;

        [AppSettingsArgument]
        [CommandLineArgument("from", ShortName = "f", Description = "Database to migrate from", ValueHint = "MONGO_URL")]
        public string FromMongoDbUrl { get; set; }

        [AppSettingsArgument]
        [CommandLineArgument("to", ShortName = "t", Description = "Database to migrate to", ValueHint = "MONGO_URL")]
        public string ToMongoDbUrl { get; set; }

        [AppSettingsArgument]
        [CommandLineArgument("overwrite", Description = "Overwrite existing entries in the target database")]
        public bool Overwrite { get; set; }

        [CommandLineArgument("help", ShortName = "?", Description = "Show this help")]
        public bool ShowUsage { get; set; }

        public override void Execute()
        {
            if (ShowUsage)
            {
                WriteMessage(Parser.LogoBanner);
                WriteMessage(Parser.Usage);
                return;
            }

            if (String.IsNullOrEmpty(FromMongoDbUrl))
            {
                WriteError("A from database URL must be given");
                return;
            }

            if (String.IsNullOrEmpty(ToMongoDbUrl))
            {
                if (!Overwrite)
                {
                    WriteError("Specify the overwrite flag if you meant to migrate to the same database");
                    return;
                }

                ToMongoDbUrl = FromMongoDbUrl;
            }

            var fromUrl = new MongoUrl(FromMongoDbUrl);
            var toUrl = new MongoUrl(ToMongoDbUrl);

            fromDb = new MongoClient(new MongoUrl(FromMongoDbUrl)).GetDatabase(fromUrl.DatabaseName);
            toDb = new MongoClient(new MongoUrl(ToMongoDbUrl)).GetDatabase(toUrl.DatabaseName);

            MigrateUsers();

            // Drop tables, rebuild collections, etc..
        }

        async Task ProcessCollection(string collectionName, Func<BsonDocument, Task<BsonDocument>> func)
        {
            var fromDocs = fromDb.GetCollection<BsonDocument>(collectionName);
            var toDocs = toDb.GetCollection<BsonDocument>(collectionName);
            long count = await fromDocs.CountAsync(new BsonDocument());
            long i = 0;
            bool inPlaceUpdate = (FromMongoDbUrl.Equals(ToMongoDbUrl));
            long copyCount = 0;
            long dropCount = 0;

            using (var cursor = await fromDocs.Find(new BsonDocument()).ToCursorAsync())
            {
                while (await cursor.MoveNextAsync())
                {
                    foreach (var fromDoc in cursor.Current)
                    {
                        BsonValue id = fromDoc["_id"];
                        BsonDocument toDoc = null;

                        if (!inPlaceUpdate)
                        {
                            // See if this id already exists in the target database
                            toDoc = await toDocs.Find(d => d["_id"] == id).FirstOrDefaultAsync();

                            // If it does, and we are not overwriting, skip
                            if (toDoc != null && !Overwrite)
                                continue;
                        }

                        toDoc = await func(fromDoc);

                        if (inPlaceUpdate)
                        {
                            // If in-place-updating, null means remove it, same reference means don't change otherwise update
                            if (toDoc == null)
                            {
                                await fromDocs.DeleteOneAsync(d => d["_id"] == id);
                                dropCount++;
                            }
                            else if (!Object.ReferenceEquals(fromDoc, toDoc))
                            {
                                copyCount++;
                                await toDocs.ReplaceOneAsync(d => d["_id"] == id, toDoc);
                            }
                        }
                        else
                        {
                            // If copying to new database, not null means copy, null means don't, i.e. delete it
                            if (toDoc == null)
                            {
                                dropCount++;
                            }
                            else
                            {
                                copyCount++;
                                await toDocs.InsertOneAsync(toDoc);
                            }
                        }

                        i++;
                        Console.Write("\r{0}... {1:##0}%", collectionName, (double)i / (double)count * 100.0);
                    }
                }
            }

            if (inPlaceUpdate)
                Console.WriteLine("\r{0}... Done.  {1} updated, {2} deleted", collectionName, copyCount, dropCount);
            else
                Console.WriteLine("\r{0}... Done.  {1} copied, {2} dropped", collectionName, copyCount, dropCount);
        }

        // Everything below this line is different for each migration
        // --8<-------------------------------------------------------------

        void MigrateUsers()
        {
            ProcessCollection("user", fromDoc => 
            {
                if (fromDoc.Contains("emailMd5Hash"))
                    return Task.FromResult(fromDoc);

                BsonDocument toDoc = fromDoc.DeepClone().AsBsonDocument;

                using (var md5 = MD5.Create())
                {
                    toDoc["emailMd5Hash"] = md5.ComputeHash(Encoding.UTF8.GetBytes(fromDoc["email"].AsString.Trim().ToLower())).ToHex();
                }

                return Task.FromResult(toDoc);
            }).Wait();
        }
    }
}


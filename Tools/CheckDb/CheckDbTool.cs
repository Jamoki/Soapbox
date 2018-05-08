using System;
using ToolBelt;
using ServiceBelt;
using Dmo = Shared.DataModel;
using System.Collections.Generic;
using ServiceStack.FluentValidation;
using System.IO;
using System.Linq;
using MongoDB.Driver;
using MongoDB.Bson;
using System.Reflection;
using ServiceStack.Validation;
using System.Collections;
using System.Threading.Tasks;

namespace Dividend
{
    [CommandLineTitle("Dividend Database Scrubber")]
    [CommandLineDescription("Scrubs the dividend.com database")]
    [CommandLineCopyright("Copyright (c) 2014, Jamoki, LLC")]
    public class CheckDbTool : ToolBase
    {
        [CommandLineArgument("help", ShortName="?", Description="Shows this help")]
        public bool ShowUsage { get; set; }

        [AppSettingsArgument]
        [CommandLineArgument("mongo", ShortName="m", Description="URL of the MongoDB server, e.g. mongo://localhost:27017")]
        public ParsedUrl MongoDbUrl { get; set; }

        [CommandLineArgument("names", ShortName = "n", Description = "Names of collections to check. Use * for all.", ValueHint = "NAME[,NAME]")]
        public List<string> CollectionNames { get; set; }

        [CommandLineArgument("fix", ShortName = "f", Description = "Fix bad references. WARNING: May cause data loss. Make sure you have a backup!")]
        public bool FixBadReferences { get; set; }

        private IMongoManager Mongo { get; set; }
        private int numErrors;
        private string logFileName = "problems.log";
        private StreamWriter Problems { get; set; }
        private Funq.Container Container { get; set; }
        private readonly Type dataMarkerType = typeof(Dmo.ISharedDataModel);

        public CheckDbTool()
        {
            CollectionNames = new List<string>();
            Container = new Funq.Container();
        }

        public override void Execute()
        {
            if (ShowUsage)
            {
                WriteMessage(Parser.LogoBanner);
                WriteMessage(Parser.Usage);
                return;
            }

            if (MongoDbUrl == null)
            {
                WriteError("MongoDB database URL must be specified");
                return;
            }

            this.Mongo = new MongoManager(new MongoDB.Driver.MongoUrl(MongoDbUrl), dataMarkerType);

            Container.Register<IMongoManager>(Mongo);
            Container.RegisterValidators(dataMarkerType.Assembly);

            using (this.Problems = new StreamWriter(logFileName))
            {
                var collections = dataMarkerType.Assembly.GetTypes()
                    .Where(t => typeof(ICollectionObject).IsAssignableFrom(t) && t != typeof(ICollectionObject)).ToDictionary(t => t.Name, StringComparer.InvariantCultureIgnoreCase);
                var collectionNames = collections.Select(t => t.Key);

                WriteMessage("Collections: {0}", StringUtility.Join(", ", collectionNames.ToList()));

                if (CollectionNames.Count == 1 && CollectionNames[0] == "*")
                {
                    CollectionNames.Clear();
                    CollectionNames.AddRange(collectionNames);
                }

                if (CollectionNames.Count == 0)
                {
                    WriteError("Specify the collections to check. Use * for all.");
                    return;
                }

                foreach (var collectionName in CollectionNames)
                {
                    Type collectionType = null;

                    if (!collections.TryGetValue(collectionName, out collectionType))
                        throw new Exception(String.Format("Collection '{0}' does not exist", collectionName));

                    MethodInfo methodInfo = this.GetType().GetMethod("CheckCollection", BindingFlags.NonPublic | BindingFlags.Instance);
                    MethodInfo genericInfo = methodInfo.MakeGenericMethod(collectionType);

                    int previousNumErrors = this.numErrors;

                    genericInfo.Invoke(this, null);

                    WriteMessage("{0} problems found", numErrors - previousNumErrors);
                }

                if (numErrors > 0)
                    WriteMessage("There were {0} database problems found. See '{1}' for details.", numErrors, logFileName);
                else
                    WriteMessage("Database checked out OK");
            }
        }

        private async Task CheckCollection<TDmo>() 
            where TDmo: ICollectionObject
        {
            string collectionName = MongoUtils.ToCamelCase(typeof(TDmo).Name);
            List<ObjectId> collectionIds = 
                (await Mongo.GetDatabase().GetCollection<BsonDocument>(collectionName)
                    .Find(new BsonDocument()).Project(Builders<BsonDocument>.Projection.Expression(d => d["_id"].AsObjectId)).ToListAsync());
            TDmo doc = default(TDmo);
            int count = collectionIds.Count;
            IValidator<TDmo> validator = null;

            WriteMessage("{0} has {1} documents", collectionName, count);

            try
            {
                validator = Container.Resolve<IValidator<TDmo>>();
            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("Problem getting validator service for {0} collection - {1}", collectionName, ex.Message), ex);
            }

            var collection = Mongo.GetCollection<TDmo>();

            for (int i = 0; i < count; i++)
            {
                try
                {
                    doc = await collection.Find(d => d.Id == collectionIds[i]).FirstAsync();
                }
                catch (Exception ex)
                {
                    if (ex is MongoConnectionException)
                    {
                        throw new Exception("Connection to MongoDB lost. There were probably too many problems being encountered.");
                    }

                    WriteProblem(collectionName, collectionIds[i], ex.Message);
                    continue;
                }

                var result = validator.Validate(doc);

                if (!result.IsValid)
                {
                    foreach (var error in result.Errors)
                    {
                        if (error.ErrorCode == "InvalidReference")
                        {
                            var referrerId = (ObjectId)error.AttemptedValue;

                            WriteProblem(collectionName, doc.Id, "{0}: ObjectId(\"{1}\") }} is missing", 
                                MongoUtils.ToCamelCase(error.PropertyName), referrerId);

                            if (FixBadReferences)
                            {
                                // Remove all the documents that refer the missing document, including this one...
                                await Mongo.DeleteReferrers((Type)error.CustomState, referrerId, (type, id) => 
                                {
                                    WriteProblem(MongoUtils.ToCamelCase(type.Name), id, "Deleting unreferred document");
                                });
                            }
                        }
                        else
                        {
                            WriteProblem(
                                collectionName, doc.Id, "\n  " + 
                                String.Join("\n  ", result.Errors.Select(e => String.Format("Property {0}: {1}", e.PropertyName, e.ErrorMessage))));
                        }
                    }
                }

                Console.Write("\r{0}... {1:##0}%", collectionName, (double)(i + 1) / count * 100.0);
            }

            Console.WriteLine("\r{0}... Done", collectionName);
        }

        private void WriteProblem(string collectionName, ObjectId id, string format, params object[] args)
        {
            Problems.WriteLine("{0} {{ _id: ObjectId(\"{1}\") }}: {2}", collectionName, id, String.Format(format, args));
            numErrors++;
        }
    }
}


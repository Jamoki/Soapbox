using System;
using System.Net;
using Api.ServiceInterface;
using MongoDB.Bson;
using MongoDB.Driver;
using Rql;
using Rql.MongoDB;
using ServiceStack;
using ServiceBelt;
using Dmo = Shared.DataModel;
using Smo = Api.ServiceModel;
using ServiceStack.Logging;

namespace Api.ServiceInterface
{
    public class ArticleService : MongoService<Smo.Article, Smo.ArticleQuery, Dmo.Article>
    {
        ILog log = LogManager.GetLogger(typeof(ArticleService));

        public ISitesManager Sites { get; set; }

        public override void BeforeValidation(Dmo.Article dmo)
        {
            Sites.AddTitleSummaryAndImage(dmo);
        }

        public override void AfterUpdate(Dmo.Article dmo)
        {
            try
            {
                Sites.UpdateArticleFiles(dmo);
            }
            catch (Exception e)
            {
                log.Error("Unable to update article", e);
            }
        }
    }
}


using System;
using System.Collections.Generic;
using System.Net.Mime;
using System.Reflection;
using System.Text;
using Dmo = Shared.DataModel;
using Smo = Api.ServiceModel;
using Api.ServiceInterface;
using ServiceStack;
using ServiceStack.Host;
using ServiceStack.Logging;
using ServiceStack.Text;
using ServiceStack.Caching;
using ServiceStack.Messaging;
using Rql.MongoDB;
using Rql;
using ServiceStack.Validation;
using MongoDB.Driver;
using MongoDB.Bson;
using Shared.DataModel;
using Funq;
using ServiceStack.Redis;
using ServiceStack.Messaging.Redis;
using System.Linq;
using ServiceBelt;

namespace ApiService
{
    public class AppHost : AppHostHttpListenerBase
    {
        ILog log = LogManager.GetLogger(typeof(AppHost));

        public AppHost() : base("Soapbox API Service", typeof(IApiServiceInterface).Assembly)
        {
        }

        public override void Configure(Funq.Container container)
        {
            JsConfig.EmitCamelCaseNames = true;

            RqlHelper.AddRqlPropertyCopiers();

            var appConfig = (ApiServiceConfig)container.Resolve<IApiServiceConfig>();

            SetConfig(
                new HostConfig
                { 
                    EnableFeatures = Feature.All & ~Feature.Soap,
                    DefaultContentType = "application/json",
                    AppendUtf8CharsetOnContentTypes = new HashSet<string>
                    {
                        "application/json"
                    },
                    DebugMode = appConfig.DebugMode,
                });

            container.Register<IRedisClientsManager>(c => new PooledRedisClientManager(appConfig.CacheHostUrl.HostAndPort));
            container.Register<ICacheClient>(c => c.Resolve<IRedisClientsManager>().GetCacheClient());
            container.Register<IMongoManager>(new MongoManager(appConfig.MongoDbUrl, typeof(ISharedDataModel)));
            container.Register<IEmailManager>(new EmailManager(appConfig));
            container.Register<ITokenManager>(new TokenManager(new Dictionary<string, string> 
            { 
                { "login", appConfig.LoginTokenSecret }
            }));
            container.RegisterAutoWiredAs<ResourceManager, IResourceManager>();
            container.RegisterAutoWiredAs<SessionManager, ISessionManager>();
            container.RegisterAutoWiredAs<SitesManager, ISitesManager>();
            container.RegisterValidators(typeof(Dmo.ISharedDataModel).Assembly, typeof(Smo.IApiServiceModel).Assembly);

            Plugins.Add(new ServiceBelt.CorsFeature(
                allowOrigins: appConfig.CorsAllowedOrigins.Select(u => u.ToString()).ToList(),
                allowHeaders: ServiceBelt.CorsFeature.DefaultHeaders + ",Accept,Authorization",
                exposeHeaders: true,
                allowCredentials: false));
            Plugins.Add(new LoginTokenFeature(container.Resolve<ITokenManager>()));

            CatchAllHandlers.Add((httpMethod, pathInfo, filePath) =>
                StaticFileHandler.Factory(pathInfo)
            );

        }

        public override ServiceStack.Host.HttpListener.ListenerRequest CreateRequest(System.Net.HttpListenerContext httpContext, string operationName)
        {
            log.InfoFormat("{0}|{1}", httpContext.Request.HttpMethod.ToUpper(), httpContext.Request.RawUrl);

            return base.CreateRequest(httpContext, operationName);
        }

        public override object OnServiceException(ServiceStack.Web.IRequest httpReq, object request, Exception ex)
        {
            log.ErrorFormat("Exception|{0}|{1}", ex.GetType().Name, ex.Message);

            return base.OnServiceException(httpReq, request, ex);
        }

        public override void Stop()
        {
            var mq = Container.Resolve<IMessageService>();

            if (mq != null)
                mq.Stop();

            base.Stop();
        }
    }
}


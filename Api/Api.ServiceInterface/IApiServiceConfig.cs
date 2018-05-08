using System;
using ServiceStack.Configuration;
using System.Collections.Generic;
using MongoDB.Driver;
using ToolBelt;

namespace Api.ServiceInterface
{
    public interface IApiServiceConfig
    {
        ParsedUrl ServiceUrl { get; }
        MongoUrl MongoDbUrl { get; }
        ParsedUrl CacheHostUrl { get; }
        ParsedUrl AwsSesSmtpUrl { get; }
        List<ParsedUrl> CorsAllowedOrigins { get; }
        ParsedUrl WebsiteUrl { get; }
        ParsedEmail SupportEmail { get; }
        string LoginTokenSecret { get; }
        bool DebugMode { get; }
    }
}


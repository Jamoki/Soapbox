using System;
using System.Configuration;
using Api.ServiceInterface;
using ServiceStack.Configuration;
using System.Collections.Generic;
using ToolBelt;
using System.Reflection;
using MongoDB;
using MongoDB.Driver;
using ServiceBelt;

namespace ApiService
{
    [CommandLineCopyright("(c) 2015, Jamoki")]
    [CommandLineTitle("Soapbox API Service")]
    [CommandLineDescription("Main Soapbox API service executable")]
    public class ApiServiceConfig : AutoServiceConfig, IApiServiceConfig, IEmailManagerConfig
    {
        [AppSettingsArgument]
        [CommandLineArgument("service", ShortName = "s", Description = "Service URL. Can include wildcard for host.  e.g. http://*:1337", ValueHint = "URL")]
        public ParsedUrl ServiceUrl { get; private set; }
        [AppSettingsArgument]
        [CommandLineArgument("database", ShortName = "d", Description = "MongoDB URL. e.g. mongodb://user:password@localhost:27017/database", ValueHint = "URL")]
        public MongoUrl MongoDbUrl { get; private set; }
        [CommandLineArgument("help", ShortName = "?", Description = "Shows this help")]
        public bool ShowHelp { get; private set; }
        [AppSettingsArgument]
        public ParsedUrl CacheHostUrl { get; private set; }
        [AppSettingsArgument]
        public ParsedUrl AwsSesSmtpUrl { get; set; }
        [AppSettingsArgument]
        public List<ParsedUrl> CorsAllowedOrigins { get; private set; }
        [AppSettingsArgument]
        public ParsedUrl WebsiteUrl { get; private set; }
        [AppSettingsArgument]
        public ParsedEmail SupportEmail { get; set; }
        [AppSettingsArgument]
        public string LoginTokenSecret { get; private set; }
        [AppSettingsArgument]
        public bool DebugMode { get; private set; }
    }
}


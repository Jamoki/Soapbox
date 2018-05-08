using System;
using System.Collections.Generic;
using System.Net;
using Api.ServiceInterface;
using MongoDB.Driver;
using Rql;
using Rql.MongoDB;
using ServiceStack;
using ServiceStack.FluentValidation;
using ToolBelt;
using ServiceBelt;
using Dmo = Shared.DataModel;
using Smo = Api.ServiceModel;
using ServiceStackService = global::ServiceStack.Service;
using System.Reflection;
using System.Linq;

namespace Api.ServiceInterface
{
    public class InfoService : ServiceStackService
    {
        public IApiServiceConfig Config { get; set; }

        public object Get(Smo.Info request)
        {
            // Get the version from the initial assembly (the service)
            Assembly assembly = Assembly.GetEntryAssembly();
            object[] attributes = assembly.GetCustomAttributes(true);
            string version = ((AssemblyFileVersionAttribute)attributes.First(x => x is AssemblyFileVersionAttribute)).Version;

            return new Smo.InfoResponse() 
            {
                ServiceVersion = version,
            };
        }
    }
}


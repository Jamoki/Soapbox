using System;
using ServiceStack;

namespace Api.ServiceModel
{
    // NOTE: Do not version this resource so that it can always be used to discover
    // information about the service.  Clients should treat every field in the 
    // response as optional except 'serviceVersion'.
    [Route("/info", "GET")]
    public class Info : IReturn<InfoResponse>
    {
    }

    public class InfoResponse
    {
        public string ServiceVersion { get; set; }
        public bool IsProduction { get; set; }
        // BUG #79: Versions of all the sub-components, like RQL, ServiceStack, MongoDB, etc..
    }
}


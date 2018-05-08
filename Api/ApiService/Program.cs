using System;
using System.Reflection;
using System.Text;
using Funq;
using Api.ServiceInterface;
using Mono.Unix;
using Mono.Unix.Native;
using ServiceStack.Logging;
using ServiceStack.Logging.NLogger;
using ServiceStack.Text;
using ToolBelt;
using System.Configuration;

namespace ApiService
{
    class Program
    {
        static int Main(string[] args)
        {
            LogManager.LogFactory = new NLogFactory();

            var appHost = new AppHost();
            var appConfig = new ApiServiceConfig();

            try
            {
                appConfig.ProcessAppSettings(ConfigurationManager.AppSettings);
                appConfig.ProcessCommandLine(args);
                appHost.Container.Register<IApiServiceConfig>(appConfig);
                appHost.Init();
            }
            catch (Exception e)
            {
                while (e != null)
                {
                    ConsoleUtility.WriteMessage(MessageType.Error, e.Message);
                    e = e.InnerException;
                }
                return -1;
            }

            appConfig.Log();
            appHost.Start(appConfig.ServiceUrl.WithPath("/").ToString());

            UnixSignal[] signals = new UnixSignal[] { 
                new UnixSignal(Signum.SIGINT), 
                new UnixSignal(Signum.SIGTERM), 
            };

            // Wait for a unix signal
            for (bool exit = false; !exit;)
            {
                int id = UnixSignal.WaitAny(signals);

                if (id >= 0 && id < signals.Length)
                {
                    if (signals[id].IsSet)
                        exit = true;
                }
            }

            return 0;
        }
    }
}

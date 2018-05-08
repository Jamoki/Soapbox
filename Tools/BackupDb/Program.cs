using System;
using System.Reflection;
using ToolBelt;

namespace Backlog
{
    public class Program
    {
        public static int Main(string[] args)
        {
            BackupDbTool tool = new BackupDbTool();

            try
            {
                tool.ProcessAppSettings();
                tool.ProcessCommandLine(args);

                tool.Execute();
                return (tool.HasOutputErrors ? 1 : 0);
            }
            catch (Exception exception)
            {
                while (exception != null)
                {
                    ConsoleUtility.WriteMessage(MessageType.Error, "{0}", exception.Message);
                    exception = exception.InnerException;
                }
                return 1;
            }
        }
    }
}
using System;
using ToolBelt;

namespace Dividend
{
    class Program
    {
        public static int Main(string[] args)
        {
            CheckDbTool tool = new CheckDbTool();

            try
            {
                tool.ProcessAppSettings();
                tool.ProcessCommandLine(args);

                tool.Execute();
                return (tool.HasOutputErrors ? 1 : 0);
            }
            catch (Exception e)
            {
                while (e != null)
                {
                    #if DEBUG
                    ConsoleUtility.WriteMessage(MessageType.Error, e.ToString());  
                    #else
                    ConsoleUtility.WriteMessage(MessageType.Error, e.Message);  
                    #endif
                    e = e.InnerException;
                }
                return 1;
            }
        }
    }
}

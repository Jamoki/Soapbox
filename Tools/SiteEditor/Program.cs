using System;
using ToolBelt;

namespace Soapbox
{
    class Program
    {
        public static int Main(string[] args)
        {
            SiteEditorTool tool = new SiteEditorTool();

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
                    ConsoleUtility.WriteMessage(MessageType.Error, e.Message);  
                    e = e.InnerException;
                }
                return 1;
            }
        }
    }
}

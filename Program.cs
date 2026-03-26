using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace XovisPaxForecastFeedWinSvc
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {
            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[]
            {
                new XovisDataSyncService()
            };
            ServiceBase.Run(ServicesToRun);

//#if DEBUG
//            XovisDataSyncService service = new XovisDataSyncService();
//            service.OnDebug();
//            Console.WriteLine("Service running in debug mode. Press ENTER to stop.");
//            Console.ReadLine();
//#else
//            ServiceBase[] ServicesToRun;
//            ServicesToRun = new ServiceBase[]
//            {
//                new XovisDataSyncService()
//            };
//            ServiceBase.Run(ServicesToRun);
//#endif
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using journal.console.lib.Consoles;
using kj.kihon;

namespace journal10
{
    class Program
    {
        static void Main(string[] args)
        {
            var log = new ErrorLogger("$error.txt");
            if (log.isValid() != true) return;
            try
            {
                Console.WriteLine($"{AppUtil.AppName}:XML,PDFのアップロード");
                var lst = ArgListUtil.createArgList(args);
                var srcdir = lst.getText('i');
                srcdir.existDir();
                
                var appTypeChar = lst.getText('t');
                journal10_util.AppType appType= journal10_util.AppType.none;
                if(appTypeChar=="kenkyu")
                    appType= journal10_util.AppType.kenkyu;
                if(appTypeChar=="journal")
                    appType= journal10_util.AppType.journal;

                if (appType == journal10_util.AppType.none)
                    throw new Exception($"AppType(/t)が定義されていない kenkyu or journal");
                
                var core = new journal10_util(log);
                core.Run(srcdir,appType);
            }
            catch (Exception ex)
            {
                log.err(AppUtil.AppName, ex.Message);
            }
            log.showdosmsg();
        }
    }
}

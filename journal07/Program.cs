using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using kj.kihon;
using journal.console.lib.Consoles;

namespace journal07
{
    class Program
    {
        static void Main(string[] args)
        {
            var log = new ErrorLogger();
            if (log.isValid() != true) return;
            try
            {
                Console.WriteLine($"{AppUtil.AppName}:ExcelのIP一覧をMySqlに登録するSQLを作成");
                var lst = ArgListUtil.createArgList(args);
                var srcdir = lst.getText('i');
                srcdir.existDir();
                var core = new journal07_util(log);
                core.Run(srcdir);
            }
            catch (Exception ex)
            {
                log.err(AppUtil.AppName, ex.Message);
            }
            log.showdosmsg();
        }
    }
}
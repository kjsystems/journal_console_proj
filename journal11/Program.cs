using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using journal.console.lib.Consoles;
using kj.kihon;

namespace journal11
{
    class Program
    {
        static void Main(string[] args)
        {

            var log = new ErrorLogger();
            if (log.isValid() != true) return;
            try
            {
                Console.WriteLine($"{AppUtil.AppName}:XMLからTXTの作成");
                Console.WriteLine($" srcxml: 入力フォルダ");
                Console.WriteLine($" txt: 出力フォルダ");
                var lst = ArgListUtil.createArgList(args);
                var srcdir = lst.getText('i');
                srcdir.existDir();
                var core = new journal11_util(log);
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

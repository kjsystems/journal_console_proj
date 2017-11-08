using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using journal.console.lib.Consoles;
using kj.kihon;

namespace journal06
{
    class Program
    {
        static void Main(string[] args)
        {
            var log = new ErrorLogger();
            try
            {
                if (args.Length == 0)
                {
                    Console.WriteLine($"{AppUtil.AppName}:入稿テキストをKJPへ変換");
                    return;
                }

                var lst = ArgListUtil.createArgList(args);
                var srcdir = lst.getText('i');
                srcdir.existDir();

                var core = new journal06_util(log);
                core.Run(srcdir);
            }
            catch (Exception ex)
            {
                Console.WriteLine("エラーがあります {0}", ex.Message);
            }
            log.showdosmsg();
        }
    }
}
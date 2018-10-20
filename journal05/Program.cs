using kj.kihon;
using System;
using journal.console.lib.Consoles;

namespace journal05
{
    class Program
    {
        static void Main(string[] args)
        {
            var log = new ErrorLogger("{AppUtil.AppName }:index/index.txtをAzure Searchへ登録");
            try
            {
                var lst = ArgListUtil.createArgList(args);
                var srcdir = lst.getText('i');
                srcdir.existDir();

                var core = new journal05_util(log);
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
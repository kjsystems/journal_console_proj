using System;
using System.Collections.Generic;
using journal.console.lib.Consoles;
using kj.kihon;

namespace journal01
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            var log = new ErrorLogger("$error.txt");
            if (log.isValid() != true) return;
            try
            {
                Console.WriteLine($"{AppUtil.AppName}:入稿テキストからXMLの作成");
                var lst = ArgListUtil.createArgList(args);
                var srcdir = lst.getText('i');
                srcdir.existDir();

                Console.WriteLine("※TXTからXMLの作成のみ（研究Ｌのsql等は作成しない）");
                var core = new journal01_util(log);
                //core.run(srcdir);
                core.RunText2Xml(srcdir);
            }
            catch (Exception ex)
            {
                log.err("kenkyu02", ex.Message);
            }
            log.showdosmsg();
        }
    }
}
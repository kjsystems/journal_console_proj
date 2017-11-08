using System;
using System.Collections.Generic;
using journal.console.lib.Consoles;
using kj.kihon;

namespace journal12
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            var log = new ErrorLogger();
            try
            {
                Console.WriteLine($"KJPからテキストの作成");
                var lst = ArgListUtil.createArgList(args);
                var srcdir = lst.getText('i');
                srcdir.existDir();

                var util = new journal12_util(log);
                util.Run(srcdir);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
            log.showdosmsg();
        }
    }
}
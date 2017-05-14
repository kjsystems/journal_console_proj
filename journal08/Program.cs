using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using journal.console.lib.Consoles;
using kj.kihon;
using kjp2idml.lib;

namespace journal08
{
  class Program
  {
    static void Main(string[] args)
    {
      var log = new ErrorLogger();
      if (log.isValid() != true) return;
      try
      {
        Console.WriteLine($"{AppUtil.AppName}:TXTからPDFまで作成");
        var lst = ArgListUtil.createArgList(args);
        var srcdir = lst.getText('i');
        srcdir.existDir();

        Console.WriteLine("TXTからKJPの作成");
        new journal06_util(log).Run(srcdir);

        Console.WriteLine("KJPからIDMLの作成");
        new CreateIdmlFromKjpFile(log).Run(srcdir,"template.idml");
      }
      catch (Exception ex)
      {
        log.err(AppUtil.AppName, ex.Message);
      }
      log.showdosmsg();
    }
  }
}

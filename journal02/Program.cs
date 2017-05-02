using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using journal.console.lib.Consoles;
using kj.kihon;

namespace journal02
{
  class Program
  {
    static void Main(string[] args)
    {
      ErrorLogger log = new ErrorLogger();
      try
      {
        Console.WriteLine($"Word(docx)からテキストへの変換");
        var lst = ArgListUtil.createArgList(args);
        var srcdir = lst.getText('i');
        srcdir.existDir();

        var core = new journal02_util(log);
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

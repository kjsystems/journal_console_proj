using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using kj.kihon;
using journal.console.lib.Consoles;

namespace journal04
{
  class Program
  {
    static void Main(string[] args)
    {
      var log = new ErrorLogger("{AppUtil.AppName }:XMLからAzureSearch用JOSNの作成");
      try
      {
        var lst = ArgListUtil.createArgList(args);
        var srcdir = lst.getText('i');
        srcdir.existDir();

        var core = new journal04_util(log);
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

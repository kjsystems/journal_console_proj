using kj.kihon;
using kj.kihon.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using wordxml.Models;

namespace journal.console.lib.Console
{
  public class journal02_util : kihon_base
  {
    public string JobDir { get; set; }

    public journal02_util(ILogger log) : base(log)
    {
    }

    void MeltWordFile(string wordpath, out string documentPath)
    {
      var zip = new ZIPUtil();
      var xmlDir = JobDir.combine("xml").createDirIfNotExist();
      var outDir = xmlDir.combine(wordpath.getFileNameWithoutExtension());
      zip.meltZip(wordpath, outDir);
      System.Console.WriteLine($"==>zip:{outDir}");

      documentPath = outDir.combine("word").combine("document.xml");
      documentPath.existFile();
      System.Console.WriteLine($"==>xml:{documentPath}");
    }

    public void Run(string jobdir)
    {
      jobdir.existDir();
      JobDir = jobdir;

      var docdir = jobdir.combine("doc");
      docdir.existDir();
      string[] srcfiles = docdir.getFiles("*.doc", false);
      if (srcfiles.Length == 0)
        throw new Exception($"docディレクトリにWORDファイルがない DIR={docdir}");

      foreach (var wordpath in srcfiles)
      {
        if (wordpath.getExtension().ToLower() == ".doc")
        {
          Log.err(wordpath, 0, "procword", "docx形式で保存してください");
          continue;
        }
        System.Console.WriteLine($"word:{wordpath}");
        //解凍する
        //戻り値は \word\document.xml
        string documentPath;
        MeltWordFile(wordpath, out documentPath);

        var parser = new WordXmlParser();
        List<WordXmlParaItem> paralst;
        parser.ProcessWordFile(documentPath, out paralst);
      }
    }
  }
}

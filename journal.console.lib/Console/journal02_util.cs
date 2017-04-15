using kj.kihon;
using kj.kihon.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

        //document.xmlをParseする
        var parser = new WordXmlParser();
        List<WordXmlParaItem> paralst;
        parser.ProcessWordFile(documentPath, out paralst);

        var sb = new StringBuilder();
        int preJisage = -1;
        int preMondo = -1;
        foreach (var para in paralst)
        {
          if(para.Jisage>0)
            sb.Append($"<字下 {para.Jisage}>");
          if (para.Jisage == 0 && preJisage > 0)
          {
            sb.Append($"<字下 0>");
            preJisage = -1;
          }
          if (para.Mondo > 0)
            sb.Append($"<問答 {para.Mondo}>");
          if (para.Mondo == 0 && preMondo > 0)
          {
            sb.Append($"<問答 0>");
            preMondo = -1;
          }

          sb.AppendLine($"{para.Text}<改行>");
          if (para.Jisage > 0)
            preJisage = para.Jisage;
          if (para.Mondo > 0)
            preMondo = para.Mondo;
        }
        var outpath = jobdir
          .combine("out")
          .createDirIfNotExist()
          .combine(wordpath.getFileNameWithoutExtension()+".txt");
        System.Console.WriteLine($"==>{outpath}");
        FileUtil.writeTextToFile(sb.ToString(),Encoding.UTF8,outpath);
      }
    }
  }
}

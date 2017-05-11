using kj.kihon;
using kj.kihon.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using wordxml.Models;

namespace journal.console.lib.Consoles
{
  public class journal02_util : kihon_base
  {
    public string JobDir { get; set; }
    public int ParagraphFontSize { get; set; }  //210とか180とか

    public journal02_util(ILogger log) : base(log)
    {
    }

    void MeltWordFile(string wordpath, out string documentPath, out string stylePath)
    {
      var zip = new ZIPUtil();
      var wordxmlDir = JobDir.combine("wordxml").createDirIfNotExist();  //解凍するディレクトリ
      var outDir = wordxmlDir.combine(wordpath.getFileNameWithoutExtension());
      zip.meltZip(wordpath, outDir);
      System.Console.WriteLine($"==>zip:{outDir}");

      documentPath = outDir.combine("word").combine("document.xml");
      documentPath.existFile();
      stylePath = outDir.combine("word").combine("styles.xml");
      stylePath.existFile();
      System.Console.WriteLine($"==>xml:{documentPath}");
    }

    void GetParagraphFontSize(string stylePath)
    {
      using (var rd = XmlReader.Create(stylePath))
      {
        XmlDocument doc = new XmlDocument();
        doc.Load(rd);
        XmlNode root = doc.DocumentElement;

        ParagraphFontSize = 210;
        var sz = root //w:styles
          .ChildNodes.Cast<XmlNode>() //w:stylesの子供
          .FirstOrDefault(s => s.Name == "w:style" && s.Attributes["w:type"].Value == "paragraph")?
          .ChildNodes.Cast<XmlNode>() //w:styleの子供
          .FirstOrDefault(s => s.Name == "w:rPr")?
          .ChildNodes.Cast<XmlNode>() //w:rPrの子供
          .FirstOrDefault(s => s.Name == "w:sz")?
          .Attributes["w:val"]
          .Value.toInt(0) * 10;
        if (sz != null)
          ParagraphFontSize = (int)sz;
        Console.WriteLine($"ParagraphFontSize={ParagraphFontSize}");
      }
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

      foreach (var wordpath in srcfiles.Select((v, i) => new { v, i }))
      {
        //開いているファイルは使わない
        if (wordpath.v.getFileNameWithoutExtension().StartsWith("~$"))
          continue;
        if (wordpath.v.getExtension().ToLower() == ".doc")
        {
          Log.err(wordpath.v, 0, "procword", "docx形式で保存してください");
          continue;
        }
        System.Console.WriteLine($"{wordpath.i + 1}/{srcfiles.Length} word:{wordpath}");
        //解凍する
        //戻り値は \word\document.xml
        MeltWordFile(wordpath.v, out string documentPath, out string stylePath);

        //paragraphの文字サイズを取得する（字下げ用）
        GetParagraphFontSize(stylePath);

        //document.xmlをParseする
        var parser = new WordXmlParser(ParagraphFontSize, Log);
        List<WordXmlParaItem> paralst;
        parser.ProcessWordFile(documentPath, out paralst);

        //テキストを作成する
        var sb = CreateTextFromParaList(paralst);

        var outpath = jobdir
        .combine("txt")
        .createDirIfNotExist()
        .combine(wordpath.v.getFileNameWithoutExtension() + ".txt");
        System.Console.WriteLine($"==>{outpath}");
        FileUtil.writeTextToFile(sb.ToString(), Encoding.UTF8, outpath);
      }
    }

    public static StringBuilder CreateTextFromParaList(List<WordXmlParaItem> paralst)
    {
      var sb = new StringBuilder();
      int preJisage = -1;
      int preMondo = -1;
      foreach (var para in paralst)
      {
        if (para.Jisage != 0)  //マイナスもあり
          sb.Append($"<字下 {para.Jisage}>");
        if (para.Jisage == 0 && preJisage > 0)
        {
          sb.Append($"<字下 0>");
          preJisage = -1;
        }
        if (para.Mondo != 0)  //マイナスもあり
          sb.Append($"<問答 {para.Mondo}>");
        if (para.Mondo == 0 && preMondo > 0)
        {
          sb.Append($"<問答 0>");
          preMondo = -1;
        }
        if (para.Align == WordXmlParaItem.AlignType.Right)
        {
          sb.Append($"<字揃 右>");
        }
        if(para.IsMidashi)
          sb.Append($"<見出>");
        sb.Append($"{para.Text}");
        if (para.IsMidashi)
          sb.AppendLine($"</見出>");
        if (!para.IsMidashi)
          sb.AppendLine($"<改行>");
        if (para.Jisage > 0)
          preJisage = para.Jisage;
        if (para.Mondo > 0)
          preMondo = para.Mondo;
      }
      return sb;
    }
  }
}

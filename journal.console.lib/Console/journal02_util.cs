using kj.kihon;
using kj.kihon.Utils;
using System;
using System.Linq;
using System.Xml;

namespace journal.console.lib.Console
{
  public class journal02_util : kihon_base
  {
    public string JobDir { get; set; }

    public journal02_util(ILogger log) : base(log)
    {
    }

    void ProcessWordFile(string wordpath)
    {
      //解凍する
      //戻り値は \word\document.xml
      string documentPath;
      MeltWordFile(wordpath, out documentPath);

      //xmlをParseする
      ParseDocumentXml(documentPath);
    }

    #region <w:r>
    void ParseWr(XmlNode parent)
    {
      foreach (XmlNode child in parent.Cast<XmlNode>()/*.Where(m => m.Name == "w:t")*/)
      {
        System.Console.WriteLine($"name={child.Name} xml={child.InnerXml}");
      }
    }
    #endregion

    #region <w:p>
    void ParseParagraph(XmlNode para)
    {
      System.Console.WriteLine($"para={para.InnerXml}");
      foreach (XmlNode wr in para.ChildNodes)  //<w:r>
      {
        ParseWr(wr);
      }
    }
    #endregion

    void ParseDocumentXml(string documentPath)
    {
      using (var rd = XmlReader.Create(documentPath))
      {
        XmlDocument doc = new XmlDocument();
        doc.Load(rd);
        XmlNode root = doc.DocumentElement;

        var w_body = root.FirstChild;//w:body
        foreach (XmlNode node in w_body.ChildNodes)
        {
          if (node.Name != "w:p") continue;
          ParseParagraph(node);
        }
#if false
        var doc = XNode.ReadFrom(rd);
        rd.MoveToContent();
        while (rd.Read())
        {
          if (rd.NodeType != XmlNodeType.Element)
          {
            continue;
          }
          var elem = XElement.ReadFrom(rd) as XElement;
          System.Console.WriteLine($"element={elem.ToString()}");
        }
#endif
      }
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

      foreach (var src in srcfiles)
      {
        if (src.getExtension().ToLower() == ".doc")
        {
          Log.err(src, 0, "procword", "docx形式で保存してください");
          continue;
        }
        System.Console.WriteLine($"word:{src}");
        ProcessWordFile(src);
      }
    }
  }
}

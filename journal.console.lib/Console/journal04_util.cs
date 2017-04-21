using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using journal.console.lib.Models;
using kj.kihon;
using Newtonsoft.Json;
using Formatting = Newtonsoft.Json.Formatting;

namespace journal.console.lib.Consoles
{
  public class journal04_util : kihon_base
  {
    public journal04_util(ILogger log) : base(log)
    {
    }

    public void Run(string srcdir)
    {
      srcdir.existDir();
      var xmldir=srcdir.combine("xml");
      xmldir.existDir();
      var idxdir = srcdir.combine("index").createDirIfNotExist();

      Console.WriteLine(xmldir);
      var idlst = new List<SearchIndex>();
      foreach (var xmlpath in xmldir.getFiles("*.xml"))
      {
        //タグから文字列だけを抽出する
        idlst.Add(new SearchIndex
        {
          Id=xmlpath.getFileNameWithoutExtension(),  //01-001
          Honbun = GetHonbunTextFromXmlPath(xmlpath)  //<文章>ないから本文だけ抽出する
        });
      }

      //JSONで書き出す
      var outpath = idxdir.combine("index.txt");
      Console.WriteLine($"==>{outpath}");
      FileUtil.writeTextToFile(JsonConvert.SerializeObject(idlst, Formatting.Indented),Encoding.UTF8,outpath);
    }

    #region XMLファイルの<文章>タグから本文だけを抽出する
    public string GetHonbunTextFromXmlPath(string xmlpath)
    {
      var sb=new StringBuilder();
      using (var rd = XmlReader.Create(xmlpath))
      {
        XmlDocument doc = new XmlDocument();
        doc.Load(rd);
        XmlNode root = doc.DocumentElement;

        foreach (XmlNode bunsho in root.ChildNodes)
        {
          //ルビとかさぼってる
          if (bunsho.Name != "文章") continue;
          //foreach (var child in node.ChildNodes.Cast<XmlElement>().Where(n => n.NodeType==XmlNodeType.Text))
          //{
            sb.Append(bunsho.InnerText);
          //}
        }
      }
      return sb.ToString();
    }
    #endregion  
  }
}

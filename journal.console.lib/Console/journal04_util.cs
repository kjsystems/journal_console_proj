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
    public int Id { get; set; }
    public journal04_util(ILogger log) : base(log)
    {
      Id = 1;
    }

    public void Run(string srcdir)
    {
      srcdir.existDir();
      var xmldir=srcdir.combine("xml");
      xmldir.existDir();
      var idxdir = srcdir.combine("index").createDirIfNotExist();

      Console.WriteLine(xmldir);
      var idxlst = new List<BunshoItem>();
      foreach (var xmlpath in xmldir.getFiles("*.xml"))
      {
        AddIndexFromXmlPath(xmlpath,ref idxlst);
      }

      //JSONで書き出す
      var outpath = idxdir.combine("index.txt");
      Console.WriteLine($"==>{outpath}");
      FileUtil.writeTextToFile(JsonConvert.SerializeObject(idxlst, Formatting.Indented),Encoding.UTF8,outpath);
    }

    #region XMLファイルの<文章>タグから本文だけを抽出する
    public void AddIndexFromXmlPath(string xmlpath,ref List<BunshoItem> idxlst)
    {
      using (var rd = XmlReader.Create(xmlpath))
      {
        XmlDocument doc = new XmlDocument();
        doc.Load(rd);
        XmlNode root = doc.DocumentElement;

        foreach (XmlNode bunsho in root.ChildNodes)
        {
          //ルビとかさぼってる
          if (bunsho.Name != "文章") continue;
          //foreach (XmlElement child in bunsho.ChildNodes.Cast<XmlElement>().Where(n => n.NodeType==XmlNodeType.Text))
          //{
            idxlst.Add(new BunshoItem
            {
              Id=Id.ToString(),
              FileName = xmlpath.getFileNameWithoutExtension(),
              Text= bunsho.InnerText,
            } );
          //}
          Id++;
        }
      }
    }
    #endregion  
  }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using journal.console.lib.Models;
using journal.lib.Models;
using journal.lib.Services;
using journal.search.lib.Models;
using kj.kihon;
using Newtonsoft.Json;
using Formatting = Newtonsoft.Json.Formatting;

namespace journal.console.lib.Consoles
{
  public class journal04_util : kihon_base
  {
    public int Id { get; set; }
    public string ContentsPath { get; set; }
    public journal04_util(ILogger log) : base(log)
    {
      Id = 1;
    }

    public void Run(string srcdir)
    {
      srcdir.existDir();
      var xmldir = srcdir.combine("xml");
      xmldir.existDir();
      ContentsPath = srcdir.combine("list").combine("contents.txt");
      ContentsPath.existFile();
      var idxdir = srcdir.combine("index").createDirIfNotExist();

      //contents一覧を読み込み
      ReadContents(out List<JournalContent> conlst);

      Console.WriteLine(xmldir);
      var idxlst = new List<BunshoResult>();
      foreach (var xmlpath in xmldir.getFiles("*.xml"))
      {
        AddIndexFromXmlPath(xmlpath,conlst, ref idxlst);
      }

      //JSONで書き出す
      var outpath = idxdir.combine("index.txt");
      Console.WriteLine($"==>{outpath}");
      FileUtil.writeTextToFile(JsonConvert.SerializeObject(idxlst, Formatting.Indented), Encoding.UTF8, outpath);
    }

    #region list/contents.txtを読み込み
    void ReadContents(out List<JournalContent> conlst)
    {
      var rd = new JournalContentsReader(Log);
      rd.ReadFromPath(ContentsPath, out conlst);
    }
    #endregion

    #region XMLファイルの<文章>タグから本文だけを抽出する
    public void AddIndexFromXmlPath(string xmlpath,List<JournalContent>conlst, ref List<BunshoResult> idxlst)
    {
      using (var rd = XmlReader.Create(xmlpath))
      {
        XmlDocument doc = new XmlDocument();
        doc.Load(rd);
        XmlNode root = doc.DocumentElement;

        var fileName = xmlpath.getFileNameWithoutExtension();   //001-01
        var go = fileName.Substring(0, 3).toInt(0);   //001 ==> 1
        var curPage = 0;
        foreach (XmlNode bunsho in root.ChildNodes)
        {
          //ルビとかさぼってる
          if (bunsho.Name != "文章") continue;
          var nodePage = bunsho.ChildNodes
            .Cast<XmlNode>()
            .FirstOrDefault(n => n.Name == "頁");
          if(nodePage!=null)
            curPage= nodePage.Attributes["内容"].Value.toInt(0);

          var content = conlst.FirstOrDefault(m => m.FileName==fileName);
          if (content == null)
          {
            Log.err(ContentsPath,0,"adindex",$"contents.txtにファイル名がない FileName={fileName}");
            continue;
          }

          idxlst.Add(new BunshoResult
          {
            Id = Id.ToString(),
            FileName = fileName,
            Go=go,
            Text = bunsho.InnerText,
            Chosha=content.Chosha,
            Title=content.Title,
            SubTitle = content.SubTitle,
            Page = curPage,
            JumpId = bunsho.Attributes["ID"].Value.toInt(0)
          });
          Id++;
        }
      }
    }
    #endregion  
  }
}

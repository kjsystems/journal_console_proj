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
        public const string FILENAME_INDEX_JSON = "index.json";
        public const string FILENAME_CONTENTS_JSON = "contents.json";
        public const string FILENAME_MOKUJI_JSON = "mokuji.json";

        public int Id { get; set; }
        public string ContentsPath { get; set; }
        public string MokujiPath { get; set; }

        public journal04_util(ILogger log) : base(log)
        {
            Id = 1;
        }

        void ReadMokuji(out List<JoMokuji> mokulst)
        {
            mokulst = new List<JoMokuji>();
            var rd = new CSVFileReader(new ErrorLogger());
            rd.setupTargetToken('\t');
            var csv = rd.readFile(MokujiPath, Encoding.UTF8, true);
            foreach (var row in csv.Rows)
            {
                mokulst.Add(new JoMokuji
                {
                    Id = row["ID"].toInt(0), Go = row["号数"], Tokushu = row["特集名"], 
                    Henshu = row["編集委員"],
                    IsDebug = row["編集中"]=="*"
                });
            }
        }

        public void Run(string srcdir)
        {
            srcdir.existDir();
            var xmldir = srcdir.combine("xml");
            xmldir.existDir();
            ContentsPath = srcdir.combine("list").combine("contents.txt");
            ContentsPath.existFile();
            MokujiPath = srcdir.combine("list").combine("mokuji.txt");
            MokujiPath.existFile();
            var idxdir = srcdir.combine("index").createDirIfNotExist();

            //contents一覧を読み込み
            Console.WriteLine($"コンテンツの読み込み");
            ReadContents(out List<JournalContent> conlst);

            //mokuji.txtを読み込み
            Console.WriteLine($"目次の読み込み");
            ReadMokuji(out List<JoMokuji> mokulst);
            
            
            Console.WriteLine(xmldir);
            var idxlst = new List<BunshoResult>();
            Console.WriteLine($"インデックスの作成");
            foreach (var xmlpath in xmldir.getFiles("*.xml"))
            {
                Path = xmlpath;
                Console.WriteLine($"==>{xmlpath.getFileName()}");
                AddIndexFromXmlPath(xmlpath, conlst, ref idxlst);
            }

            //JSONで書き出す
            var outpath = idxdir.combine(FILENAME_INDEX_JSON);
            Console.WriteLine($"==>{outpath}");
            FileUtil.writeTextToFile(JsonConvert.SerializeObject(idxlst, Formatting.Indented), Encoding.UTF8, outpath);

            outpath = idxdir.combine(FILENAME_CONTENTS_JSON);
            Console.WriteLine($"==>{outpath}");
            FileUtil.writeTextToFile(JsonConvert.SerializeObject(conlst, Formatting.Indented), Encoding.UTF8, outpath);
            
            outpath = idxdir.combine(FILENAME_MOKUJI_JSON);
            Console.WriteLine($"==>{outpath}");
            FileUtil.writeTextToFile(JsonConvert.SerializeObject(mokulst, Formatting.Indented), Encoding.UTF8, outpath);
        }

        #region list/contents.txtを読み込み

        void ReadContents(out List<JournalContent> conlst)
        {
            Console.WriteLine($"{ContentsPath}");
            var rd = new JournalContentsReader(Log);
            rd.ReadFromPath(ContentsPath, out conlst);
        }

        #endregion

        #region XMLファイルの<文章>タグから本文だけを抽出する

        public void AddIndexFromXmlPath(string xmlpath, List<JournalContent>conlst, ref List<BunshoResult> idxlst)
        {
            try
            {
                var rd = XmlReader.Create(xmlpath);
                XmlDocument doc = new XmlDocument();
                doc.Load(rd);
                XmlNode root = doc.DocumentElement;

                var fileName = xmlpath.getFileNameWithoutExtension(); //001-01
                var go = fileName.Substring(0, 3).toInt(0); //001 ==> 1
                var curPage = 0;
                foreach (XmlNode bunsho in root.ChildNodes)
                {
                    //ルビとかさぼってる
                    if (bunsho.Name != "文章") continue;

                    AddBunsho(bunsho, fileName, go, conlst, ref curPage, ref idxlst);
                }
            }
            catch (Exception ex)
            {
                Log.err(Path,0,"addindex",ex.Message);
            }
        }

        #endregion

        void AddBunsho(XmlNode bunsho, string fileName, int go, List<JournalContent>conlst, ref int curPage, ref List<BunshoResult> idxlst)
        {
            try
            {
                var nodePage = bunsho.ChildNodes
                    .Cast<XmlNode>()
                    .FirstOrDefault(n => n.Name == "頁");
                if (nodePage != null)
                    curPage = nodePage.Attributes["内容"].Value.toInt(0);

                var content = conlst.FirstOrDefault(m => m.FileName == fileName);
                if (content == null)
                {
                    Log.err(ContentsPath, 0, "adindex", $"contents.txtにファイル名がない FileName={fileName}");
                    return;
                }

                idxlst.Add(new BunshoResult
                {
                    Id = Id.ToString(),
                    FileName = fileName,
                    Go = go,
                    Text = bunsho.InnerText.trimZen(),
                    Chosha = content.Chosha,
                    Title = content.Title,
                    SubTitle = content.SubTitle,
                    Page = curPage,
                    JumpId = bunsho.Attributes["ID"].Value.toInt(0)
                });
                Id++;
            }
            catch (Exception ex)
            {
                 Log.err(Path,0,"addbunsho",ex.Message);   
            }
        }
    }
}
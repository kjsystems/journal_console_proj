using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using journal.console.lib.Models;
using kj.kihon;
using kjlib.zip.Models;
using Microsoft.VisualBasic.Logging;
using wordxml.Models;
using kj.wordlib;
using kj.kihon.Utils;

namespace journal.console.lib.Consoles
{
    public class journal02_util : kihon_base
    {
        public string JobDir { get; set; }
        private string OutMeltWordDir { get; set; }

        List<Rule> RuleList { get; set; }

        public journal02_util(ILogger log) : base(log)
        {
        }

        public void MeltFromStream(Stream wordStream, string outMeltDir)
        {
            if (wordStream == null || wordStream.Length == 0)
                throw new ArgumentNullException("Streamgaがnullまたは長さがゼロ");

            var zip = new ZIPUtil();
            OutMeltWordDir = outMeltDir;

            zip.meltZip(wordStream, outMeltDir);
            System.Console.WriteLine($"==>zip:{outMeltDir}");
        }

        public void MeltFromWordFile(
            string wordpath, /*, out string documentPath, out string endnotesPath, out string stylePath*/
            string outMeltDir)
        {
            var zip = new ZIPUtil();
            OutMeltWordDir = outMeltDir;

            wordpath.existFile();
            outMeltDir.existDir();

            zip.meltZip(wordpath, outMeltDir);
            System.Console.WriteLine($"==>zip:{outMeltDir}");
        }

        //Wordの解凍ディレクトリからテキストを取得する
        public void ParseWordMeltDir(out string sb)
        {
            //document.xmlをParseする
            var parser = new WordXmlParser(RuleList,Log);
            parser.ProcessWordFile(OutMeltWordDir);

            //テキストを作成する
            sb = CreateTextFromParaList(parser.ParaList)
                .Replace("✱", "＊");
            sb = ReplaceGaijiToShotai(sb);
        }

        string ReplaceGaijiToShotai(string buf)
        {
            string[] lst = {"赢"};
            foreach (var ch in lst)
            {
                buf = buf.Replace(ch, $"<書体 \"Adobe Song Std/L\">{ch}</書体>");
            }

            return buf;
        }

        // docxで保存する
        void SaveDocx(string docdir)
        {
            var util = new WordUtil(Log);
            foreach (var docpath in docdir.getFiles("*.doc", false))
            {
                if (docpath.getExtension().ToLower() == ".docx")
                    continue;
                var docxpath = docpath.getDirectoryName().combine(docpath.getFileNameWithoutExtension() + ".docx");
                Console.WriteLine($"==>{docxpath}");

                //docxで保存
                util.SaveAsDocx(docpath, docxpath);

                //docは移動する
                //System.IO.File.Move(docpath,docdir.combine("out").createDirIfNotExist().combine((docpath.getFileName())));
            }

            util.quit();
        }

        // Wordファイル単体で処理
        public void RunWordPath(string jobdir, IEnumerable<string> wordlst)
        {
            foreach (string wordpath in wordlst)
            {
                //解凍する
                //戻り値は \word\document.xml
                var wordMeltDir = wordpath.getDirectoryName()
                    .combine("wordxml")
                    .createDirIfNotExist()
                    .combine(wordpath.getFileNameWithoutExtension())
                    .createDirIfNotExist();
                MeltFromWordFile(wordpath, wordMeltDir);

                //Wordの解凍ディレクトリからテキストを取得する
                ParseWordMeltDir(out string sb);

                var outpath = jobdir
                    .combine("txt")
                    .createDirIfNotExist()
                    .combine(wordpath.getFileNameWithoutExtension() + ".txt");
                Console.WriteLine($"==>{outpath}");
                FileUtil.writeTextToFile(sb.ToString(), Encoding.UTF8, outpath);
            }
        }

        // Wordファイルをフォルダで処理
        public void Run(string jobdir)
        {
            jobdir.existDir();
            JobDir = jobdir;

            var docdir = jobdir.combine("doc");
            docdir.existDir();

            //docをdocxで保存する
            SaveDocx(docdir);

            string[] srcfiles = docdir.getFiles("*.docx", false);
            if (srcfiles.Length == 0)
                throw new Exception($"docディレクトリにWORDファイルがない DIR={docdir}");

            // ルールを読み込み
            var rd = new RuleReader(Log);
            var stylePath = jobdir.getUpDir().combine("styles").combine("style-word.txt");
            rd.Read(stylePath, out List<Rule> rulelst);
            RuleList = rulelst;
            Console.WriteLine($"Rules Count={RuleList.Count} {stylePath}");

            foreach (var wordpath in srcfiles.Select((v, i) => new {v, i}))
            {
                //開いているファイルは使わない
                if (wordpath.v.getFileNameWithoutExtension().StartsWith("~$"))
                    continue;
                Console.WriteLine($"{wordpath.i + 1}/{srcfiles.Length} word:{wordpath.v}");
                RunWordPath(jobdir, new List<string> {wordpath.v});
            }
        }

        int _preJisage = 0;
        int _preMondo = 0;

        public string CreateTextFromPara(WordXmlParaItem para)
        {
            var sb = new StringBuilder();
            //1行目だけインデント
            if (para.Jisage == -para.Mondo)
            {
                if (para.Jisage >= 20)
                {
                    sb.Append($"<字揃 右>");
                }
                else
                {
                    if (_preJisage != 0)
                        sb.Append($"<字下 0>");
                    if (_preMondo != 0)
                        sb.Append($"<問答 0>");
                    //全角SPを個数分挿入する
                    sb.Append(new string('　', para.Jisage));
                }

                _preJisage = 0;
                _preMondo = 0;
            }
            else
            {
                if (para.Jisage != _preJisage) //マイナスもあり
                    sb.Append($"<字下 {para.Jisage}>");
                if (para.Mondo != _preMondo) //マイナスもあり
                    sb.Append($"<問答 {para.Mondo}>");
                _preJisage = para.Jisage;
                _preMondo = para.Mondo;
            }

            if (para.Align == WordXmlParaItem.AlignType.Right)
            {
                sb.Append($"<字揃 右>");
            }

            if (para.IsParaStyle)
                sb.Append($"<スタ \"{para.StyleName}\">");
            sb.Append($"{para.Text}");
            if (para.IsParaStyle)
                sb.Append($"</スタ>");
            sb.Append($"<改行>");
            //_preJisage = para.Jisage;
            //_preMondo = para.Mondo;
            return sb.ToString();
        }

        public string CreateTextFromParaList(List<WordXmlParaItem> paralst)
        {
            var sb = new StringBuilder();
            foreach (var para in paralst)
            {
                var buf = CreateTextFromPara(para);

                //style-word.txtの##スタイル置換でタグに置き換える
                //WORDスタイルを置き換える

                //WORDの段落スタイルを置換する
                // <スタ "見出し 1">　　　　一　東北院供養願文、女院彰子「かな願文」</スタ>
                // →　　<ス字 小見出し>一　東北院供養願文、女院彰子「かな願文」</ス字>

                var rule = RuleList.FirstOrDefault(m => m.Name == "スタイル置換");
                if (rule != null)
                {
                    // カンマ区切りで3個あれば置換する
                    // 見出し 1,　　<小見出し>,</小見出し>  ==>  Wordのスタイル"見出し 1"を<小見出し>...</小見出し>に置き換える
                    foreach (var line in rule.ValueList)
                    {
                        var tokens = line.Split(',');
                        if(tokens.Length<3) continue;

                        buf = buf.ReplaceParaStyle(tokens[0], tokens[1], tokens[2]);
                    }
                }
                // 著者を置き換え
                buf = buf.ReplaceParaStyleChosha();

                sb.AppendLine(buf);
            }

            return sb.ToString();
        }
    }
}
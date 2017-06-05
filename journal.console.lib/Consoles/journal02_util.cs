using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using kj.kihon;
using kjlib.zip.Models;
using Microsoft.VisualBasic.Logging;
using wordxml.Models;

namespace journal.console.lib.Consoles
{
    public class journal02_util : kihon_base
    {
        public string JobDir { get; set; }
        public int ParagraphFontSize { get; set; } //210とか180とか

        private string OutMeltWordDir { get; set; }
        private string OutMeltWordDirSubWord => OutMeltWordDir.combine("word");

        public string WordXmlDocumentPath => OutMeltWordDirSubWord.combine("document.xml");
        public string WordXmlEndnotesPath => OutMeltWordDirSubWord.combine("endnotes.xml");
        public string WordXmlStylesPath => OutMeltWordDirSubWord.combine("styles.xml");

        public journal02_util(ILogger log) : base(log)
        {
        }

        public void MeltFromStream(Stream wordStream, string outMeltDir)
        {
            var zip = new ZIPUtil();
            //var wordxmlDir = JobDir.combine("wordxml").createDirIfNotExist(); //解凍するディレクトリ
            //var outMeltDir = outDir.combine(wordpath.getFileNameWithoutExtension());
            OutMeltWordDir = outMeltDir;

            zip.meltZip(wordStream, outMeltDir);
            System.Console.WriteLine($"==>zip:{outMeltDir}");

        }

        void MeltFromWordFile(
            string wordpath /*, out string documentPath, out string endnotesPath, out string stylePath*/)
        {
            var zip = new ZIPUtil();
            var wordxmlDir = JobDir.combine("wordxml").createDirIfNotExist(); //解凍するディレクトリ
            var outMeltDir = wordxmlDir.combine(wordpath.getFileNameWithoutExtension());
            OutMeltWordDir = outMeltDir;

            zip.meltZip(wordpath, outMeltDir);
            System.Console.WriteLine($"==>zip:{outMeltDir}");

            //documentPath = outMeltDir.combine("word").combine("document.xml");
            //documentPath.existFile();
            //endnotesPath = outMeltDir.combine("word").combine("endnotes.xml");
            //endnotesPath.existFile();
            //stylePath = outMeltDir.combine("word").combine("styles.xml");
            //stylePath.existFile();
            //System.Console.WriteLine($"==>xml:{documentPath}");
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
                    ParagraphFontSize = (int) sz;
                Console.WriteLine($"ParagraphFontSize={ParagraphFontSize}");
            }
        }

        //Wordの解凍ディレクトリからテキストを取得する
        public void ParseWordMeltDir(out string sb)
        {
            //paragraphの文字サイズを取得する（字下げ用）
            GetParagraphFontSize(WordXmlStylesPath);

            //document.xmlをParseする
            var parser = new WordXmlParser(ParagraphFontSize, Log);
            parser.ProcessWordFile(WordXmlDocumentPath, WordXmlEndnotesPath);

            //テキストを作成する
            sb = CreateTextFromParaList(parser.ParaList);
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
                MeltFromWordFile(wordpath.v);

                //Wordの解凍ディレクトリからテキストを取得する
                ParseWordMeltDir(out string sb);

                var outpath = jobdir
                .combine("txt")
                .createDirIfNotExist()
                .combine(wordpath.v.getFileNameWithoutExtension() + ".txt");
                System.Console.WriteLine($"==>{outpath}");
                FileUtil.writeTextToFile(sb.ToString(), Encoding.UTF8, outpath);
            }
        }

        public static string CreateTextFromParaList(List<WordXmlParaItem> paralst)
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
                if (para.IsMidashi)
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
            return sb.ToString();
        }
    }
}

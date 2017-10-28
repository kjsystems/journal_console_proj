using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using journal.console.lib.Models;
using journal.lib.Models;
using kj.kihon;
using Microsoft.VisualBasic.Logging;
using Newtonsoft.Json;
using wordxml.Models;

namespace journal.console.lib.Consoles
{
    public class journal09_util : kihon_base
    {
        private string SrcDir { get; set; }
        string XmlDir => SrcDir.combine("xml").createDirIfNotExist();
        string JsonDir => SrcDir.combine("json").createDirIfNotExist();

        public void RunText2Xml(string srcdir)
        {
            SrcDir = srcdir;

            // 入稿の複数ファイルからTXTにまとめる
            CreateTxtFiles();

            // txt→xmlに作成する
            CreateXmlFiles();

        }

        // テキストファイルをつなげる
        string LinkTextFiles(string[] filelst)
        {
            var sb = new StringBuilder();
            foreach (var path in filelst)
            {
                sb.Append(FileUtil.getTextFromPath(path, Encoding.UTF8));
            }
            return sb.ToString();
        }


        private void CreateTxtFiles()
        {
            var srcdir = SrcDir;
            var nyukodir = srcdir.combine("入稿");
            //先にタグのチェック
            foreach (var path in nyukodir.getFiles("*.txt"))
            {
                CreateWakaXmlCore core = new CreateWakaXmlCore(Log);
                core.run(path, "", true);
            }

            // 入稿→txtに3桁のファイルでまとめる
            string[] lst = {"000", "004", "005", "006"};
            foreach (var fileName in lst)
            {
                string[] filelst = nyukodir.getFiles($"{fileName}*.txt", false);
                if (filelst.Length == 0)
                {
                    Log.err("journal09", $"対象のファイルはない fileName={fileName} dir={nyukodir}");
                    continue;
                }
                //ファイルをつなげる
                var outpath = srcdir.combine("txt")
                    .createDirIfNotExist()
                    .combine($"{fileName}.txt");
                FileUtil.writeTextToFile(LinkTextFiles(filelst), Encoding.UTF8, outpath);
                Console.WriteLine($"==>{outpath}");
            }
        }

        private void CreateXmlFiles()
        {
            var srcdir = SrcDir;
            var txtdir = srcdir.combine("txt");
            var xmldir = XmlDir;
            string[] srclst = txtdir.getFiles("*.txt");
            foreach (var srcpath in srclst)
            {
                // ファイル名を変換する
                // 02-01gomihumihiko.xml ==> 002-01 rename
                var fname = ChangeFileName(srcpath.getFileNameWithoutExtension());

                var outpath = xmldir.combine(fname + ".xml");
                Console.WriteLine($"==>{outpath}");
                //XMLへ出力
                CreateWakaXmlCore core = new CreateWakaXmlCore(Log);
                core.run(srcpath, outpath);

                var xmlutil = new XMLUtil(Log);
                xmlutil.checkXmlFile(outpath);
            }
        }

        // 02-01gomihumihiko.xml ==> 002-01
        // 研究ジャーナル はそのまま001.xml
        string ChangeFileName(string fname)
        {
            if (Regex.IsMatch(fname, @"[0-9]{3}"))
            {
                return fname;
            }

            var match = Regex.Match(fname, @"^([0-9]{2}-[0-9]{2})");
            if (match != null)
            {
                var grp = RegexUtil.getGroup(match, 1);
                return $"0{grp}";
            }
            return fname;
        }

        public journal09_util(ILogger log) : base(log)
        {
        }
    }
}
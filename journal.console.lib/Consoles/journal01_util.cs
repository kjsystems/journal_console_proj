using System;
using System.Text.RegularExpressions;
using journal.console.lib.Models;
using kj.kihon;
using Microsoft.VisualBasic.Logging;

namespace journal.console.lib.Consoles
{
    public class journal01_util : kihon_base
    {
        public void RunText2Xml(string srcdir)
        {
            var txtdir = srcdir.combine("txt");
            var xmldir = srcdir.combine("xml").createDirIfNotExist();
            string[] srclst = txtdir.getFiles("*.txt");
            foreach (var srcpath in srclst)
            {
                // ファイル名を変換する
                // 02-01gomihumihiko.xml ==> 002-01 rename
                var fname = ChangeFileName( srcpath.getFileNameWithoutExtension());
                
                var outpath = xmldir.combine(fname + ".xml");
                Console.WriteLine($"==>{outpath}");
                //XMLへ出力
                CreateWakaXmlCore core = new CreateWakaXmlCore(Log);
                core.run(srcpath, outpath);
            }

        }

        // 02-01gomihumihiko.xml ==> 002-01
        string ChangeFileName(string fname)
        {
            var match = Regex.Match(fname,@"^([0-9]{2}-[0-9]{2})");
            if (match != null)
            {
                var grp = RegexUtil.getGroup(match, 1);
                return $"0{grp}";
            }
            return fname;
        }

        public journal01_util(ILogger log) : base(log)
        {
        }
    }
}
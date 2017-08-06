using System;
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
                var outpath = xmldir.combine(srcpath.getFileNameWithoutExtension()+".xml");
                Console.WriteLine($"==>{outpath}");
                //XMLへ出力
                CreateWakaXmlCore core = new CreateWakaXmlCore(Log);
                core.run(srcpath, outpath);
            }

        }

        public journal01_util(ILogger log) : base(log)
        {
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using journal.console.lib.Models;
using kj.kihon;

namespace journal.console.lib.Consoles
{
    public class journal11_util : kihon_base
    {
        public journal11_util(ILogger log) : base(log)
        {
        }

        string CreateTextFromParaList(List<ParaItem> paralst)
        {
            var sb=new StringBuilder();
            foreach (var para in paralst)
            {
                sb.AppendLine($"{para.Text}<改行>");
            }
            return sb.ToString();
        }

        public void Run(string srcdir)
        {
            srcdir.existDir();
            var xmldir = srcdir.combine("srcxml");
            xmldir.existDir();
            var outdir = srcdir.combine( "txt").createDirIfNotExist();

            string[] srcfiles = xmldir.getFiles("*.xml");
            foreach (var path in srcfiles)
            {
                Console.WriteLine($"{path}");
                // XMLの読み込み
                var jo03 = new journal03_util(Log);
                jo03.ParseDocumentXml(path);

                // TXTへ書き出し
                var outpath = outdir.combine(path.getFileNameWithoutExtension()+".txt");
                var txt = CreateTextFromParaList(jo03.ParaList);

                Console.WriteLine($"==>{outpath}");
                FileUtil.writeTextToFile(txt,Encoding.UTF8,outpath);
            }
        }
    }
}

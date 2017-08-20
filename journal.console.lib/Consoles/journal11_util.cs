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

        string ChangeText(string txt)
        {
            var sb=new StringBuilder();
            var taglst = TagTextUtil.parseText(txt);
            foreach (var tag in taglst)
            {
                if (tag.getName() == "頁")
                {
                    sb.Append($"<頁 {tag.getValue("内容")}>");
                    continue;
                }
                sb.Append(tag.ToString());
            }

            return sb.ToString();
        }

        string CreateTextFromParaList(List<ParaItem> paralst)
        {
            var sb=new StringBuilder();
            var preJisage = 0;
            var preMondo = 0;
            foreach (var para in paralst)
            {
                if (para.Jisage != preJisage)
                {
                    sb.Append($"<字下 {para.Jisage}>");
                    preJisage = para.Jisage;
                }
                if (para.Mondo != preMondo)
                {
                    sb.Append($"<問答 {para.Mondo}>");
                    preMondo = para.Mondo;
                }
                if (para.IsJisoroe)
                {
                    sb.Append($"<揃字 右>");
                }

                //XMLのタグのままなので適宜修正
                var txt = ChangeText(para.Text);

                sb.AppendLine($"{txt}<改行>");
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

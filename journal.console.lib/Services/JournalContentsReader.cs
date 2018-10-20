using System.Collections.Generic;
using System.Text;
using journal05.Models;
using kj.kihon;

namespace journal.console.lib.Services
{
    public class JournalContentsReader : kihon_base
    {
        public JournalContentsReader(ILogger log) : base(log)
        {
        }

        public void ReadFromPath(string path, out List<JournalContent> lst)
        {
            lst = new List<JournalContent>();
            var rd = new CSVFileReader(new ErrorLogger());
            rd.setupTargetToken('\t');
            var appdir = AppUtil.AppPath.getDirectoryName();
            var csv = rd.readFile(path, Encoding.UTF8, true);

            var id = 1;
            foreach (var row in csv.Rows)
            {
                lst.Add(new JournalContent
                {
                    Id = id++,
                    MokujiId = row["目次ID"].toInt(0),
                    FileName = row["ファイル名"],
                    Title = row["論文名"],
                    SubTitle = row["サブタイトル"],
                    Chosha = row["執筆者名"],
                    ChoshaYomi = row["執筆者よみ"],
                    IsDebug = row["編集中"] == "*",
                    //PdfPageNum = row["PDFページ数"].toInt(0)
                });
            }
        }
    }
}
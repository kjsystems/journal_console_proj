using System;
using journal.console.lib.Consoles;
using kj.kihon;
using Microsoft.VisualBasic.Logging;

namespace journal08
{
    class Program
    {
        static void Main(string[] args)
        {
            var log = new ErrorLogger();
            if (log.isValid() != true) return;
            try
            {
                Console.WriteLine($"{AppUtil.AppName}:TXTからPDFまで作成");
                var lst = ArgListUtil.createArgList(args);

                // ディレクトリで処理
                var srcdir = lst.getText('i');
                if (!string.IsNullOrEmpty(srcdir))
                {
                    srcdir.existDir();
                }

                // kjpから(TXT→KJPをSKIP)
                var fromKjp = lst.getText('k').toInt(0) > 0 ? true : false;

                // kjpから(TXT→KJPをSKIP)
                var fromSjisKjp = lst.getText('s').toInt(0) > 0 ? true : false;

                var templateFileName = !string.IsNullOrEmpty(lst.getText('t')) ? lst.getText('t') : "template.idml";

                var util = new journal08_util(log);
                if (srcdir.existDir(false))
                {
                    util.RunForJobDir(srcdir, templateFileName, fromKjp, fromSjisKjp, log);
                    return;
                }

                // テキスト単体で処理
                var txtpath = lst.getText('e');
                if (txtpath.existFile(false))
                {
                    srcdir= txtpath.getDirectoryName().getUpDir();

                    if (fromSjisKjp)
                    {
                        throw new Exception("sjisのファイル単体オプションは無効");
                    }

                    var kjpdir = srcdir.combine("kjp");
                    kjpdir.createDirIfNotExist();

                    var kjppath = kjpdir.combine(txtpath.getFileNameWithoutExtension() + ".kjp");
                    new journal06_util(log).RunFromPath(txtpath, kjppath);
                    if (log.ErrorCount > 0)
                    {
                        throw new Exception("journal08 中断します");
                    }

                    util.RunForKjpPath(kjppath, templateFileName, fromKjp, log);
                }
            }
            catch (Exception ex)
            {
                log.err(AppUtil.AppName, ex.Message);
            }
            log.showdosmsg();
        }
    }
}
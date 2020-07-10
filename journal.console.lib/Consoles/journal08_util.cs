using System;
using System.Text;
using inddtool.shared;
using kj.kihon;
using kjp2idml.lib;

namespace journal.console.lib.Consoles
{
    public class journal08_util : kihon_base
    {
        InddTool _inddTool;

        public journal08_util(ILogger log) : base(log)
        {
        }

        void InitApp()
        {
            _inddTool = new InddTool();
            _inddTool.StartApp();
        }

        void FinishApp()
        {
            _inddTool.FinishApp();
        }


        // ディレクトリで処理
        public void RunForJobDir(string srcdir, string templateFileName, bool fromKjp, bool fromSjisKjp,string preset,
            ErrorLogger log)
        {
            var kjpdir = srcdir.combine("kjp");

            // kjp-sjis ==> kjp
            if (fromSjisKjp == true)
            {
                kjpdir.createDirIfNotExist();
                var sjisdir = srcdir.combine("kjp-sjis");
                sjisdir.existDir();
                foreach (var txtpath in sjisdir.getFiles("*.kjp"))
                {
                    Console.WriteLine($"{txtpath}");
                    var outpath = kjpdir.combine(txtpath.getFileName());
                    Console.WriteLine($"==>{outpath}");

                    var sb = new StringBuilder();
                    sb.Append(CharUtil.sjis2utf(FileUtil.getTextFromPath(txtpath, EncodingUtil.SJIS)));
                    FileUtil.writeTextToFile(sb.ToString(), Encoding.UTF8, outpath);
                }
            }

            // txt ==> kjp
            if (fromKjp != true)
            {
                // TXTから全部処理
                var txtdir = srcdir.combine("txt");
                txtdir.existDir();
                foreach (var txtpath in txtdir.getFiles("*.txt"))
                {
                    var kjppath = kjpdir.combine(txtpath.getFileNameWithoutExtension() + ".kjp");
                    new journal06_util(log).RunFromPath(txtpath, kjppath);
                    if (log.ErrorCount > 0)
                    {
                        throw new Exception("journal08 中断します");
                    }
                }
            }

            //  KJPからあと全部作る
            foreach (var kjppath in kjpdir.getFiles("*.kjp"))
            {
                RunForKjpPath(kjppath, templateFileName, false/*fromKjp*/,preset, log);
            }
        }

        // ファイル1個処理
        public void RunForKjpPath(string kjppath, string templateFileName, bool fromKjp, string preset,ErrorLogger log)
        {
            var srcdir = kjppath.getDirectoryName().getUpDir();
//            var kjpdir = srcdir.combine("kjp");
//            var kjppath = kjpdir.combine(txtpath.getFileNameWithoutExtension() + ".kjp");
//
//            if (fromKjp != true)
//            {
//                Console.WriteLine("TXTからKJPの作成");
//                new journal06_util(log).RunFromPath(txtpath, kjppath);
//                if (log.ErrorCount > 0)
//                {
//                    throw new Exception("journal08 中断します");
//                }
//            }

            Console.WriteLine("KJPからIDMLの作成");
            var kjp2idml = new CreateIdmlFromKjpFile(log);
            var idmlTemplatePath = srcdir
                .getUpDir()  //１つ上に上がる
                .combine("template")
                .combine(templateFileName);
            kjp2idml.InitTemplate(idmlTemplatePath);

            var idmlPath = srcdir.combine("outidml").combine($"{kjppath.getFileNameWithoutExtension()}.idml");
            kjp2idml.RunFromKjpPath(kjppath, srcdir, idmlPath);

            return;

            InitApp();
            Console.WriteLine("IDMLからINDDの作成");
            var inddPath = srcdir
                .combine("indd")
                .createDirIfNotExist()
                .combine($"{kjppath.getFileNameWithoutExtension()}.indd");
            _inddTool.SaveAsIndd(idmlPath, inddPath);

            Console.WriteLine("INDDからPDFの作成");
            var pdfPath = srcdir
                .combine("pdf")
                .createDirIfNotExist()
                .combine($"{kjppath.getFileNameWithoutExtension()}.pdf");
            _inddTool.ExportPdfFromIndd(inddPath, preset, pdfPath);

            FinishApp();
        }
    }
}
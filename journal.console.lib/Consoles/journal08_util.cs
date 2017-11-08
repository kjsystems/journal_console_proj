using System;
using System.Text;
using idmltool.shared;
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
        public void RunForJobDir(string srcdir, string templateFileName, bool fromKjp, bool fromSjisKjp,
            ErrorLogger log)
        {
            var kjpdir = srcdir.combine("kjp");

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


            // TXTはSKIP, KJPから全部処理
            if (fromKjp == true)
            {
                kjpdir.existDir();
                foreach (var txtpath in kjpdir.getFiles("*.kjp"))
                {
                    RunForTextPath(txtpath, templateFileName, fromKjp, log);
                }
                return;
            }

            // TXTから全部処理
            var txtdir = srcdir.combine("txt");
            txtdir.existDir();
            foreach (var txtpath in txtdir.getFiles("*.txt"))
            {
                RunForTextPath(txtpath, templateFileName, fromKjp, log);
            }
        }

        // ファイル1個処理
        public void RunForTextPath(string txtpath, string templateFileName, bool fromKjp, ErrorLogger log)
        {
            var srcdir = txtpath.getDirectoryName().getUpDir();
            var kjpdir = srcdir.combine("kjp");
            var kjppath = kjpdir.combine(txtpath.getFileNameWithoutExtension() + ".kjp");

            if (fromKjp != true)
            {
                Console.WriteLine("TXTからKJPの作成");
                new journal06_util(log).RunFromPath(txtpath, kjppath);
                if (log.ErrorCount > 0)
                {
                    throw new Exception("journal08 中断します");
                }
            }

            Console.WriteLine("KJPからIDMLの作成");
            var kjp2idml = new CreateIdmlFromKjpFile(log);
            var idmlTemplatePath = srcdir
                .combine("template")
                .combine(templateFileName);
            kjp2idml.InitTemplate(idmlTemplatePath);

            var idmlPath = srcdir.combine("outidml").combine($"{txtpath.getFileNameWithoutExtension()}.idml");
            kjp2idml.RunFromKjpPath(kjppath, srcdir, idmlPath);

            InitApp();
            Console.WriteLine("IDMLからINDDの作成");
            var inddPath = srcdir.combine("indd").combine($"{txtpath.getFileNameWithoutExtension()}.indd");
            _inddTool.SaveAsIndd(idmlPath, inddPath);

            Console.WriteLine("INDDからPDFの作成");
            var pdfPath = srcdir.combine("pdf").combine($"{txtpath.getFileNameWithoutExtension()}.pdf");
            _inddTool.ExportPdfFromIndd(inddPath, "" /*preset*/, pdfPath);

            FinishApp();
        }
    }
}
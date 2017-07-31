using System;
using idmltool.shared;
using journal.console.lib.Consoles;
using kj.kihon;
using kjp2idml.lib;

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
                if (srcdir.existDir(false))
                {
                    RunForJobDir(srcdir,log);
                }

                // テキスト単体で処理
                var txtpath = lst.getText('e');
                if (txtpath.existFile(false))
                {
                    RunForTextPath(txtpath,log);
                }

            }
            catch (Exception ex)
            {
                log.err(AppUtil.AppName, ex.Message);
            }
            log.showdosmsg();
        }

        static void RunForJobDir(string srcdir, ErrorLogger log)
        {
            Console.WriteLine("TXTからKJPの作成");
            new journal06_util(log).Run(srcdir);
            if (log.ErrorCount>0)
            {
                throw new Exception("journal08 中断します");
            }

            Console.WriteLine("KJPからIDMLの作成");
            new CreateIdmlFromKjpFile(log).Run(srcdir, "template.idml");

            var inddTool =new InddTool();
            inddTool.StartApp();
            
            Console.WriteLine("IDMLからINDDの作成");
            inddTool.SaveAsIndd(new []{$"{srcdir}\\outidml"});

            Console.WriteLine("INDDからPDFの作成");
            inddTool.ExportPdfFromIndd(new[] { $"/i{srcdir}\\indd" });
        }

        static void RunForTextPath(string txtpath, ErrorLogger log)
        {

            var srcdir = txtpath.getDirectoryName().getUpDir();
            var kjpdir = srcdir.combine("kjp");
            var kjppath = kjpdir.combine(txtpath.getFileNameWithoutExtension()+".kjp");
            
            Console.WriteLine("TXTからKJPの作成");
            new journal06_util(log).RunFromPath(txtpath,kjppath);
            if (log.ErrorCount>0)
            {
                throw new Exception("journal08 中断します");
            }

            Console.WriteLine("KJPからIDMLの作成");
            var kjp2idml = new CreateIdmlFromKjpFile(log);
            var idmlTemplatePath = srcdir.combine("template").combine("template.idml");
            kjp2idml.InitTemplate(idmlTemplatePath);

            var idmlPath = srcdir.combine("outidml").combine($"{txtpath.getFileNameWithoutExtension()}.idml");
            kjp2idml.RunFromKjpPath(kjppath,srcdir, idmlPath);

            Console.WriteLine("IDMLからINDDの作成");
            var inddTool =new InddTool();
            inddTool.StartApp();
            var inddPath = srcdir.combine("indd").combine($"{txtpath.getFileNameWithoutExtension()}.indd");
            inddTool.SaveAsIndd(idmlPath, inddPath);

            Console.WriteLine("INDDからPDFの作成");
            var pdfPath = srcdir.combine("pdf").combine($"{txtpath.getFileNameWithoutExtension()}.pdf");
            inddTool.ExportPdfFromIndd(inddPath,""/*preset*/,pdfPath);
        }
        
    }
}

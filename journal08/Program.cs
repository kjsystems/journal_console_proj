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
                var srcdir = lst.getText('i');
                srcdir.existDir();

                Console.WriteLine("TXTからKJPの作成");
                new journal06_util(log).Run(srcdir);

                Console.WriteLine("KJPからIDMLの作成");
                new CreateIdmlFromKjpFile(log).Run(srcdir, "template.idml");

                //Console.WriteLine("IDMLからPDFの作成");
                //new CreatePdfFilesFromInddDir().Run(srcdir.combine("outidml"), "", srcdir.combine("pdf").createDirIfNotExist());

                Console.WriteLine("IDMLからINDDの作成");
                InddTool.SaveAsIndd(new []{$"{srcdir}\\outidml"});

                Console.WriteLine("INDDからPDFの作成");
                InddTool.ExportPdfFromIndd(new[] { $"/i{srcdir}\\indd" });

            }
            catch (Exception ex)
            {
                log.err(AppUtil.AppName, ex.Message);
            }
            log.showdosmsg();
        }
    }
}

using System;
using System.Linq;
using System.Net;
using azureblob.Model;
using kj.kihon;
using kjlib.zip.Models;

namespace journal.console.lib.Consoles
{
    public class journal10_util : kihon_base
    {
        public enum AppType
        {
            kenkyu,
            journal,none
        }

        public journal10_util(ILogger log) : base(log)
        {
        }

        public void Run(string jobdir,journal10_util.AppType appType)
        {
            jobdir.existDir();
            var xmlDir = jobdir.combine("xml");
            xmlDir.existDir();
            var zipDir = jobdir.combine("zip");
            zipDir.createDirIfNotExist();
            var pdfDir = jobdir.combine("pdf"); //あれば
            var htmDir = jobdir.combine("html"); //あれば
            var imageDir = jobdir.combine("images"); //あれば

            //収載一覧を読み込み
            //var util02 = new kenkyu02_util(Log);
            //var shomeiLst = util02.readShomeiList(jobdir);   //ファイル名の変換に使用する

            //Azure呼び出し
            Console.WriteLine("Azureへ接続");
            var azure = new AzureBlobUtil();
            var connectionString = "webtoshoConnectionString";
            azure.CreateBlobClient(connectionString);

            var container = azure.CreateDirectory(appType.ToString());

            Console.WriteLine($"XMLからZIPを作成");
            //xmlを圧縮
            //ファイル名を　頼政集 中.XML→4.xmlに変換
            var srclst = xmlDir.getFiles("*.xml");
            var zip = new ZIPUtil();
            foreach (var item in srclst.Select((v, i) => new {v, i}))
            {
                //XMLのファイル名からZIPにファイル名を変換する 頼政集 中.XML ==> 4.zip
                var xmlPath = item.v;
                var outpath = zipDir.combine(xmlPath.getFileNameWithoutExtension() + ".zip");
                Console.WriteLine($"{xmlPath.getFileName()} ==> {outpath.getFileName()}");
                zip.createZipForFile(item.v, outpath);
            }

            //zipをAzureへ登録
            Console.WriteLine($"Azureへアップロード(ZIP)");
            foreach (var item in zipDir.getFiles("*.zip").Select((v, i) => new {v, i}))
            {
                Console.WriteLine($"==>{item.v.getFileName()}");
                azure.UploadFile(container, item.v);
            }
            if (pdfDir.existDir(false))
            {
                Console.WriteLine($"Azureへアップロード(PDF)");

                //ファイル名を変更する
                foreach (var path in pdfDir.getFiles("*.pdf", false))
                {
                    var newFileName = journal09_util.ChangeFileName(path.getFileName());
                    if(newFileName==path.getFileName())
                        continue;
                    var newPath = path
                        .getDirectoryName()
                        .combine(newFileName+".pdf");
                    Console.WriteLine($"==>{newPath}");
                    System.IO.File.Move(path,newPath);
                }
                
                foreach (var item in pdfDir.getFiles("*.pdf",false).Select((v, i) => new { v, i }))
                {
                    Console.WriteLine($"==>{item.v.getFileName()}");
                    azure.UploadFile(container, item.v);
                }
            }
            else
            {
                Console.WriteLine("PDFディレクトリはないのでコピーしない");
            }

            var containerImage = azure.CreateDirectory(appType.ToString() + "-images");
            Console.WriteLine($"Azureへアップロード container={containerImage.Name}");
            foreach (var item in imageDir.getFiles("*.*",false).Select((v, i) => new { v, i }))
            {
                Console.WriteLine($"==>{item.v.getFileName()}");
                azure.UploadFile(containerImage, item.v);
            }
        }
    }
}

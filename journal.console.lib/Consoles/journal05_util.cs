using azureblob.Model;
using journal.console.lib.Models;
using kj.kihon;
using System;
using System.Threading.Tasks;

namespace journal.console.lib.Consoles
{
    public class journal05_util : kihon_base
    {
        public journal05_util(ILogger log) : base(log)
        {
        }

        #region index/index.jsonを読みこんでAzure Searchに登録

        public void Run(string srcdir)
        {
            srcdir.existDir();
            var idxdir = srcdir.combine("index");
            idxdir.existDir();

            //contents.jsonAzure Blobに登録
            var conpath = idxdir.combine(journal04_util.FILENAME_CONTENTS_JSON);
            UploadContentsJson(conpath).Wait();

            var mokupath = idxdir.combine(journal04_util.FILENAME_MOKUJI_JSON);
            UploadContentsJson(mokupath).Wait();

            //Azure Searchにインデックスを登録
            var idxpath = idxdir.combine(journal04_util.FILENAME_INDEX_JSON);
            idxpath.existFile();
            CreateAzureSearchIndex(idxpath);
        }

        #endregion

        #region Azure Blobにアップロードする

        async Task UploadContentsJson(string conpath)
        {
            Console.WriteLine("{0}", "Upload json to Blob ...");
            Console.WriteLine("{0}", conpath);
            var az = new AzureBlobUtil();
            az.CreateBlobClient("webtoshoConnectionString");
            var container = await az.CreateDirectoryAsync("journal");
            await az.UploadFile(container, conpath);
        }

        #endregion

        #region Azure Searchにインデックスを登録

        private void CreateAzureSearchIndex(string idxpath)
        {
            var ser = new AzureSearchCreateIndex();

            // const string tblname = "journalhonbuns";
            // string searchServiceName = "webtosho-webwaka2";
            // string adminApiKey = "8968D6CEA9ADBA5E3C43A742B8AAB369";

            // リソースグループ kjsystems.net→webtoshokan→wakajiten
            string adminApiKey = "C92DA471242046F68D79810B56A3325F";
            string searchServiceName = "wakajiten";
            const string tblname = "journal-honbun";

            //Indexを作成する
            ser.InitSearchServiceClient(adminApiKey, searchServiceName);
            ser.DeleteIndexIfExists(tblname);
            ser.CreateIndex<BunshoResult>(tblname);

            //JSONを登録する
            ser.InitIndexClient(tblname);
            ser.UploadDocuments<BunshoResult>(idxpath);
        }

        #endregion
    }
}
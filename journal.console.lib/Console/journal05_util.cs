using System;
using azureblob.Model;
using azuresearch.lib.Models;
using journal.search.lib.Models;
using kj.kihon;

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
      UploadContentsJson(conpath);

      //Azure Searchにインデックスを登録
      var idxpath = idxdir.combine(journal04_util.FILENAME_INDEX_JSON);
      idxpath.existFile();
      CreateAzureSearchIndex(idxpath);
    }
    #endregion

    #region Azure Blobにアップロードする
    void UploadContentsJson(string conpath)
    {
      Console.WriteLine("{0}", "Upload json to Blob ...");
      Console.WriteLine("{0}", conpath);
      var az = new AzureBlobUtil();
      az.CreateClient("webtoshoConnectionString");
      var container = az.CreateDirectory("journal");
      az.UploadFile(container,conpath);
    }
    #endregion

    #region Azure Searchにインデックスを登録
    private void CreateAzureSearchIndex(string idxpath)
    {
      var ser = new AzureSearchCreateIndex();

      const string tblname = "journalhonbuns";
      string searchServiceName = "webtosho-journal";
      string adminApiKey = "64790FEA0044163D5F55563D54D1B73E";

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

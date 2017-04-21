using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using journal.console.lib.Models;
using kj.kihon;
using Microsoft.Azure.Search;
using Microsoft.Azure.Search.Models;
using Newtonsoft.Json;

namespace journal.console.lib.Consoles
{
  public class journal05_util : kihon_base
  {
    string searchServiceName = "webtosho-journal";
    string adminApiKey = "64790FEA0044163D5F55563D54D1B73E";

    public journal05_util(ILogger log) : base(log)
    {
    }

    public void Run(string srcdir)
    {
      srcdir.existDir();
      var idxdir = srcdir.combine("index");
      idxdir.existDir();
      var idxpath = idxdir.combine("index.txt");
      idxpath.existFile();

      var serviceClient = CreateSearchServiceClient();
      Console.WriteLine("{0}", "Deleting index...");

      const string tblname = "journalhonbuns";
      DeleteJournalHonbunsIndexIfExists(serviceClient, tblname);

      Console.WriteLine("{0}", "Creating index...");
      CreateJournalHonbunsIndex(serviceClient, tblname);

      ISearchIndexClient indexClient = serviceClient.Indexes.GetClient(tblname);

      Console.WriteLine("{0}", "Uploading documents...");
      UploadDocuments(indexClient, idxpath);

      ISearchIndexClient indexClientForQueries = CreateSearchIndexClient(tblname);

      //return;
      RunQueries(indexClientForQueries);
    }

    SearchServiceClient CreateSearchServiceClient()
    {

      var serviceClient = new SearchServiceClient(searchServiceName, new SearchCredentials(adminApiKey));
      serviceClient.AcceptLanguage = "ja-JP";
      return serviceClient;
    }
    private void DeleteJournalHonbunsIndexIfExists(SearchServiceClient serviceClient, string tblname)
    {

      if (serviceClient.Indexes.Exists(tblname))
      {
        serviceClient.Indexes.Delete(tblname);
      }
    }
    private void CreateJournalHonbunsIndex(SearchServiceClient serviceClient, string tblname)
    {
      var definition = new Index()
      {
        Name = tblname,
        Fields = FieldBuilder.BuildForType<BunshoItem>(),
        //Tokenizers = new List<Tokenizer> { new NGramTokenizer("my") },
      };
      serviceClient.Indexes.Create(definition);

      Console.WriteLine($"index {tblname} exists={serviceClient.Indexes.Exists(tblname)}");
    }
    private void UploadDocuments(ISearchIndexClient indexClient, string idxpath)
    {
      var lst = JsonConvert.DeserializeObject<List<BunshoItem>>(FileUtil.getTextFromPath(idxpath, Encoding.UTF8));
      Console.WriteLine($"UploadDocuments num={lst.Count}");

      var batch = IndexBatch.Upload(lst.Take(1000));

      try
      {
        indexClient.Documents.Index(batch);
      }
      catch (IndexBatchException e)
      {
        // Sometimes when your Search service is under load, indexing will fail for some of the documents in
        // the batch. Depending on your application, you can take compensating actions like delaying and
        // retrying. For this simple demo, we just log the failed document keys and continue.
        Console.WriteLine(
          "Failed to index some of the documents: {0}",
          String.Join(", ", e.IndexingResults.Where(r => !r.Succeeded).Select(r => r.Key)));
      }

      Console.WriteLine("Waiting for documents to be indexed...");
      Thread.Sleep(2000);
    }

    ISearchIndexClient CreateSearchIndexClient(string tblname)
    {
      var queryApiKey = "487BA0BE8FC7CA3133DA972481222D40";
      var indexClient = new SearchIndexClient(searchServiceName, tblname, new SearchCredentials(queryApiKey));
      return indexClient;
    }

    void RunQueries(ISearchIndexClient indexClient)
    {
      SearchParameters parameters;
      DocumentSearchResult<BunshoItem> results;

      parameters =
        new SearchParameters()
        {
          SearchMode= SearchMode.All,
          HighlightFields = new[] { "text" },
          Select = new[] { "id", "fileName", "text" }  //探すフィールド
        };

      results = indexClient.Documents.Search<BunshoItem>("\"藤原定家\" \"古今和歌集\"", parameters);

      WriteDocuments(results);
    }
    private static void WriteDocuments(DocumentSearchResult<BunshoItem> searchResults)
    {
      foreach (SearchResult<BunshoItem> result in searchResults.Results)
      {
        Console.WriteLine($"id={result.Document.Id} filename={result.Document.FileName} text={result.Document.Text}");
        /*foreach (var hl in result.Highlights)
        {
          Console.WriteLine($"key={hl.Key} value={string.Join("|", hl.Value)}");
        }*/
      }
      Console.WriteLine($"==>{searchResults.Results.Count}件");
    }

  }
}

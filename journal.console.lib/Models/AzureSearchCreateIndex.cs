using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.Azure.Search;
using Microsoft.Azure.Search.Models;
using Newtonsoft.Json;

namespace journal.console.lib.Models
{
    public class AzureSearchCreateIndex
    {
        private ISearchIndexClient indexClient;
        private SearchServiceClient serviceClient;

        string GetTextFromPath(string path, Encoding enc)
        {
            if (System.IO.File.Exists(path) != true)
            {
                throw new Exception("読み込むファイルがない PATH=" + path);
            }
            var res = "";
            using (System.IO.StreamReader sr = new System.IO.StreamReader(path, enc))
            {
                res = sr.ReadToEnd();
                sr.Close();
            }
            return res;
        }

        public void InitIndexClient(string queryApiKey, string searchServiceName, string tblname)
        {
            indexClient = new SearchIndexClient(searchServiceName, tblname, new SearchCredentials(queryApiKey));
        }

        public void InitIndexClient(string tblname)
        {
            indexClient = serviceClient.Indexes.GetClient(tblname);
        }

        public void InitSearchServiceClient(string adminApiKey, string searchServiceName)
        {
            serviceClient = new SearchServiceClient(searchServiceName, new SearchCredentials(adminApiKey));
            serviceClient.AcceptLanguage = "ja-JP";
        }

        public void DeleteIndexIfExists(string tblname)
        {
            Console.WriteLine("{0}", $"Deleting index... tblname={tblname}");
            try
            {
                if (serviceClient.Indexes.Exists(tblname))
                {
                    serviceClient.Indexes.Delete(tblname);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"error msg={ex.Message}");
            }
        }

        public void CreateIndex<T>(string tblname)
        {
            Console.WriteLine("{0}", $"Creating index...  tblname={tblname}");
            try
            {
//                Microsoft.Azure.Search.Models.SerializePropertyNamesAsCamelCaseAttribute attr = null;

                var definition = new Index()
                {
                    Name = tblname,
                    Fields = FieldBuilder.BuildForType<T>(),
                };
                serviceClient.Indexes.Create(definition);

                Console.WriteLine($"index {tblname} exists={serviceClient.Indexes.Exists(tblname)}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"error msg={ex.Message}");
            }
        }

        #region JSONをアップロードする

        public void UploadDocuments<T>(string jsonPath) where T : class
        {
            var lst = JsonConvert.DeserializeObject<List<T>>(GetTextFromPath(jsonPath, Encoding.UTF8));
            UploadDocuments<T>(lst);
        }

        #endregion

        #region JSONをアップロードする

        public void UploadDocuments<T>(List<T> lst) where T : class
        {
            Console.WriteLine("{0}", "Uploading documents...");
            if (indexClient == null)
                throw new Exception("ISearchIndexClientが作成されていない");
            Console.WriteLine($"UploadDocuments num={lst.Count}");

            var page = 0;
            const int per = 1000;
            while (page * per < lst.Count)
            {
                Console.Write($"page*{per}={(page + 1) * per}                    \r");
                var batch = IndexBatch.Upload(lst.Skip(page * per).Take(per));
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
                page++;
            }

            Console.WriteLine("Waiting for documents to be indexed...");
            Thread.Sleep(2000);
        }

        #endregion
    }
}
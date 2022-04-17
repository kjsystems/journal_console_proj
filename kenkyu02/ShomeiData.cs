using System.IO;
using Newtonsoft.Json;

namespace kenkyu.lib.Model
{
    /**
	山口さん作成の収載一覧.txtから読み込み
		*/
    public class ShomeiData
    {
        public int Bango { get; set; }
        public string Shomei { get; set; }
        public string Chosha { get; set; }
        public string FileName { get; set; }

        public ShomeiData()
        {
            Bango = -1;
            Shomei = "";
            Chosha = "";
        }
        [JsonIgnore]
        public string IndexDir { get { return string.Format("{0:000}", Bango); } }
        [JsonIgnore]
        public string TextDir { get { return Path.Combine(IndexDir, "text"); } }
        [JsonIgnore]
        public string ImageDir { get { return Path.Combine(IndexDir, "image"); } }
    }
}

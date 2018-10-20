namespace journal05.Models
{
    public class JournalContent
    {
        public int Id { get; set; }
        public int MokujiId { get; set; }
        public int Level { get; set; }
        public string Href { get; set; }

        public string FileName { get; set; }
        public string Title { get; set; }
        public string SubTitle { get; set; }
        public string Chosha { get; set; }
        public string ChoshaYomi { get; set; }
        public bool IsDebug { get; set; }
    }
}
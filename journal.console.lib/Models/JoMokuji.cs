namespace journal05.Models
{
    public class JoMokuji
    {
        public int Id { get; set; }

        //号数
        public string Go { get; set; }

        //特集タイトル
        public string Tokushu { get; set; }

        //編集委員
        public string Henshu { get; set; }

        public bool IsDebug { get; set; }

        public override string ToString()
        {
            return $"{Go} {Tokushu} {Henshu}";
        }
    }
}
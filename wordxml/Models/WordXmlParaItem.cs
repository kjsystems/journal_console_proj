using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace wordxml.Models
{
    public class WordXmlParaItem
    {
        public int Gyo { get; set; }
        public int Jisage { get; set; }
        public int Mondo { get; set; }

        public enum AlignType
        {
            Left,
            Center,
            Right
        }

        public AlignType Align { get; set; } = AlignType.Left;
        // スタイルが割り当てられている
        public bool IsParaStyle { get; set; }
        public string StyleName { get; set; }

        public string Text { get; set; } //ルビタグを含むタグテキスト
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace journal.console.lib.Models
{
    public class ParaItem
    {
        public int Gyo { get; set; }
        public string Text { get; set; }

        public int Jisage { get; set; } = 0;
        public int Mondo { get; set; } = 0;
        public bool IsJisoroe { get; set; } = false;

        public string CreateText()
        {
            return $"{Text}<改行>";
        }

        public static string CreateText(List<ParaItem> paralst)
        {
            var sb = new StringBuilder();
            foreach (var para in paralst)
            {
                sb.Append(para.CreateText() + "\r\n");
            }
            return sb.ToString();
        }
    }
}
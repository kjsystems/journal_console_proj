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
    public string Text { get; set; }//ルビタグを含むタグテキスト

  }
}

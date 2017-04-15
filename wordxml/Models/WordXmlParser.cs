using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace wordxml.Models
{
  public class WordXmlParser
  {
    List<WordXmlParaItem> ParaList { get; set; }
    public void ProcessWordFile(string documentPath, out List<WordXmlParaItem> paralst)
    {
      ParaList = new List<WordXmlParaItem>();
      paralst = ParaList;

      //xmlをParseする
      ParseDocumentXml(documentPath);
      Console.WriteLine($"==> paras.count={paralst.Count}");
      //foreach (var para in paralst)
      //{
      //  Console.WriteLine($"==> {para.Text}");
      //}
    }

    #region <w:t>の中のテキスト
    string ParseTextElement(XmlNode node)
    {
      var sb = new StringBuilder();
      if (node.NodeType== XmlNodeType.Text)
      {
        sb.Append(node.Value);
        return sb.ToString();
      }
      Console.WriteLine($"text以外 name={node.Name} inner={node.InnerXml}");

      foreach (XmlNode child in node.ChildNodes)
      {
        sb.Append(ParseTextElement(child as XmlElement));
      }
      return sb.ToString();
    }
    #endregion

    #region <w:p>
    /*
http://officeopenxml.com/WPparagraph.php
<w:p>
  <w:pPr>
    <w:pStyle> w:val="NormalWeb"/>
    <w:spacing w:before="120" w:after="120"/>
  </w:pPr>
  <w:r>
    <w:rPr xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main"><w:rFonts w:hint="eastAsia" /></w:rPr>
    <w:t xml"space="preserve">I feel that there is much to be said for the Celtic belief that the souls of those whom we have lost are held captive in some inferior being...</w:t>
  </w:r>
  <w:r>...</w:r>
</w:p>
     * */
    void ParseParagraph(XmlNode w_para)
    {
      //paraのプロパティはない時もあり
      if (w_para.ChildNodes.Cast<XmlNode>().Any(m => m.Name == "w:pPr"))
      {
        var w_prop = w_para.ChildNodes.Cast<XmlNode>().First();
      }

      //runは複数
      var sb = new StringBuilder();
      foreach (var w_run in w_para.ChildNodes.Cast<XmlNode>().Where(m => m.Name == "w:r"))  //<w:r>
      {
        if (w_run.ChildNodes.Cast<XmlNode>().Any(m => m.Name == "w:t") != true)
          continue;
        var w_txt = w_run.ChildNodes.Cast<XmlNode>().First(m => m.Name == "w:t");
        foreach (XmlNode node in w_txt.ChildNodes)
        {
          sb.Append(ParseTextElement(node));  //textとかrubyとか
        }
      }
      ParaList.Last().Text = sb.ToString();
    }
    #endregion

    void ParseDocumentXml(string documentPath)
    {
      using (var rd = XmlReader.Create(documentPath))
      {
        XmlDocument doc = new XmlDocument();
        doc.Load(rd);
        XmlNode root = doc.DocumentElement;

        var w_body = root.FirstChild;//w:body
        foreach (XmlNode node in w_body.ChildNodes)
        {
          if (node.Name != "w:p") continue;
          ParaList.Add(new WordXmlParaItem());
          ParseParagraph(node);
        }
      }
    }
  }
}
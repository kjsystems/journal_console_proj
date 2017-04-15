using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Xml;
using kj.kihon;

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
    }

    #region <w:t>の中のテキスト
    string ParseTextElement(XmlNode node)
    {
      var sb = new StringBuilder();
      if (node.NodeType != XmlNodeType.Text)
      {
        return $"<!-- 未対応 name=({node.Name}) -->";
      }
      sb.Append(node.Value);
      return sb.ToString();
    }
    #endregion

    #region 子供の一覧
    IEnumerable<XmlNode> getChilds(XmlNode node, string name)
    {
      return node.ChildNodes.Cast<XmlNode>().Where(m => m.Name == name);
    }
    #endregion

    #region 最初の子供
    XmlNode getFirstOfChilds(XmlNode node, string name)
    {
      return getChilds(node, name)?.FirstOrDefault();
    }
    #endregion

    int? getAttrInt(XmlNode node, string attrName)
    {
      return getAttrText(node,attrName)?.toInt(0);
    }
    string getAttrText(XmlNode node, string attrName)
    {
      return node.Attributes?[attrName]?.Value;
    }

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
      var w_prop = getFirstOfChilds(w_para, "w:pPr");
      if (w_prop!=null)
      {
        foreach (XmlNode node in w_prop.ChildNodes)
        {
          if (node.Name == "w:ind")
          {
            //<w:pPr><w:ind w:firstLineChars="500" w:firstLine="1050"/></w:pPr>  //五字下げ
            var fl = getAttrInt(node, "w:firstLine");  //1050 ==> 210で一字分
            if (fl != null)
            {
              ParaList.Last().Jisage = (int)fl / 210;
            }
            //<w:ind w:left="880" w:hanging="440"/>   //2字下げ＋問答２字
            var left = getAttrInt(node, "w:left");
            var hanging = getAttrInt(node, "w:hanging");
            if (left!=null && hanging != null)
            {
              ParaList.Last().Jisage = (int)hanging / 210;
              ParaList.Last().Mondo = ((int)left - (int)hanging) / 210;
            }
            else if (left != null)
            {
              ParaList.Last().Jisage = (int)left / 210;
            }
          }
        }
      }

      //runは複数
      var sb = new StringBuilder();
      var runlst = getChilds(w_para, "w:r");
      if (runlst == null)
        throw new Exception($"<w:r>がない para={w_para.InnerXml}");
      foreach (var run in runlst)  //<w:r>
      {
        sb.Append(ParseRun(run));
      }
      ParaList.Last().Text = sb.ToString();
    }
    #endregion

    #region <w:r>
    private string ParseRun(XmlNode w_run)
    {
      var sb = new StringBuilder();
      //<w:rPr>...</w:rPr>の中にルビ等あり
      var r_prop = getFirstOfChilds(w_run, "w:rPr");
      if (r_prop != null)
      {
        foreach (XmlNode prop in r_prop.ChildNodes)
        {
          Console.WriteLine($"run prop name={prop.Name}");
          if (prop.Name == "w:u") sb.Append("<下線>");
        }
      }

      var w_text = getFirstOfChilds(w_run, "w:t");
      if (w_text != null)
      {
        foreach (XmlNode node in w_text.ChildNodes)
        {
          var text = ParseTextElement(node);
          sb.Append(text); //textとかrubyとか
        }
      }

      if (r_prop != null)
      {
        foreach (XmlNode prop in r_prop.ChildNodes)
        {
          Console.WriteLine($"run prop name={prop.Name}");
          if (prop.Name == "w:u") sb.Append("</下線>");
        }
      }

      return sb.ToString();
    }
    #endregion

    #region Parse Xml Path
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
    #endregion
  }
}
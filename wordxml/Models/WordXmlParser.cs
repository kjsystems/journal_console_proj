using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using kj.kihon;
using kj.kihon.Utils;
using Microsoft.VisualBasic.ApplicationServices;

namespace wordxml.Models
{
    public class WordXmlParser : kihon_base
    {
        public WordXmlParser(int paragraphFontSize, ILogger log) : base(log)
        {
            ParagraphFontSize = paragraphFontSize;
            ParaList = new List<WordXmlParaItem>();
            InstrList = new List<string>();
        }

        private int ChushakuIndex { get; set; } = 1;
        private int ParagraphFontSize { get; set; }

        public List<WordXmlParaItem> ParaList { get; set; }

        /*
         * 縦中横などはフィールド文字列
      <w:r><w:rPr><w:rFonts w:ascii="ＭＳ 明朝" w:cs="Times New Roman"/><w:color w:val="auto"/></w:rPr>
        <w:fldChar w:fldCharType="begin"/></w:r>
        <w:r><w:rPr><w:rFonts w:ascii="ＭＳ 明朝" w:cs="Times New Roman"/><w:color w:val="auto"/></w:rPr>
        <w:instrText>eq \o(\s\do5(</w:instrText></w:r>
        <w:r><w:rPr><w:rFonts w:hint="eastAsia"/></w:rPr>
        <w:instrText>１</w:instrText></w:r>
        <w:r><w:rPr><w:rFonts w:ascii="ＭＳ 明朝" w:cs="Times New Roman"/><w:color w:val="auto"/></w:rPr>
        <w:instrText>),\s\do-5(</w:instrText></w:r>
        <w:r><w:rPr><w:rFonts w:hint="eastAsia"/></w:rPr>
        <w:instrText>１</w:instrText></w:r>
        <w:r><w:rPr><w:rFonts w:ascii="ＭＳ 明朝" w:cs="Times New Roman"/><w:color w:val="auto"/></w:rPr>
        <w:instrText>))</w:instrText></w:r>
        <w:r><w:rPr><w:rFonts w:ascii="ＭＳ 明朝" w:cs="Times New Roman"/><w:color w:val="auto"/></w:rPr>
        <w:fldChar w:fldCharType="end"/></w:r>
         * */
        private bool IsFldChar { get; set; }

        private List<string> InstrList { get; set; }

        public void ProcessWordFile(string documentPath, string endnotesPath)
        {
            //xmlをParseする
            ParseDocumentXml(documentPath);
            if (File.Exists(endnotesPath))
                ParseEndnotesXml(endnotesPath);
        }

        #region <w:t>の中のテキスト

        string ParseTextElement(XmlNode node)
        {
            var sb = new StringBuilder();
            if (node.NodeType != XmlNodeType.Text)
            {
                if (node.Name == "#significant-whitespace")
                    return " ";
                Log.err("parse", $"未対応のタグ name={node.Name} inner={node.InnerXml}");
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
            return getAttrText(node, attrName)?.toInt(0);
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
        public string ParseParagraph(XmlNode w_para)
        {
            var fontSize = ParagraphFontSize;
            //paraのプロパティはない時もあり
            var w_prop = getFirstOfChilds(w_para, "w:pPr");
            if (w_prop != null)
            {
                foreach (XmlNode node in w_prop.ChildNodes)
                {
                    if (node.Name == "w:jc" && !string.IsNullOrEmpty(node.Attributes?["w:val"].Value))
                    {
                        switch (node.Attributes["w:val"].Value)
                        {
                            case "left":
                                ParaList.Last().Align = WordXmlParaItem.AlignType.Left;
                                break;
                            case "center":
                                ParaList.Last().Align = WordXmlParaItem.AlignType.Center;
                                break;
                            case "right":
                                ParaList.Last().Align = WordXmlParaItem.AlignType.Right; //下揃え
                                break;
                        }
                    }
                    if (node.Name == "w:rPr" &&
                        node.Cast<XmlNode>()
                            .FirstOrDefault(m => m.Name == "w:rFonts")?
                            .Attributes?["w:eastAsiaTheme"]?
                            .Value == "majorEastAsia")
                    {
                        ParaList.Last().IsMidashi = true;
                    }

                    if (node.Name == "w:ind")
                    {
                        if (ParaList.Count == 0)
                        {
                            ParaList.Add(new WordXmlParaItem());
                        }
                        ParaList.Last().Jisage = 0;
                        //<w:p w:rsidR="006F4F91" w:rsidRDefault="008D23FE" w:rsidP="00967B89"><w:pPr><w:ind w:leftChars="200" w:left="3465" w:hangingChars="900" w:hanging="2835"/>
                        var lc = getAttrInt(node, "w:leftChars"); //200 //字下げ ==> 2字
                        if (lc != null)
                        {
                            ParaList.Last().Jisage = (int) lc / 100;
                        }
                        var flc = getAttrInt(node, "w:firstLineChars"); //200 //1行目 字下げ ==> 2字
                        if (flc != null)
                        {
                            ParaList.Last().Jisage += (int) flc / 100; //足し算なので注意
                            ParaList.Last().Mondo -= (int) flc / 100;
                        }
                        var hc = getAttrInt(node, "w:hangingChars"); //900 //ぶらさげ ==> 9字
                        if (hc != null)
                        {
                            //lc = getAttrInt(node, "w:leftChars");  //200 //字下げ ==> 2字
                            ParaList.Last().Mondo = (int) hc / 100 - ParaList.Last().Jisage;
                        }
                    }
                }
            }

            //runは複数
            var sb = new StringBuilder();
            var runlst = getChilds(w_para, "w:r");
            if (runlst == null)
                throw new Exception($"<w:r>がない para={w_para.InnerXml}");
            foreach (var run in runlst) //<w:r>
            {
                sb.Append(ParseRun(run));
            }
            //同じタグの組み合わせは消去
            return sb.ToString()
                    .Replace("</上付><上付>", "")
                    .Replace("</下付><下付>", "")
                    .Replace("</太字><太字>", "")
                    .Replace("</上線><上線>", "")
                ;
        }

        #endregion

        string getInstrTextFromWR(XmlNode wrun)
        {
            foreach (XmlNode node in wrun.ChildNodes)
            {
                if (node.Name == "w:instrText")
                {
                    return node.InnerText;
                }
            }
            return "";
        }

        string getInstrText()
        {
            //ルビ
//            if (InstrList.Count == 5 && InstrList[0].IndexOf(@"eq \o\ac(\s\up") >= 0)
            if (InstrList.Count == 5 && Regex.IsMatch(InstrList[0], @"eq \\o\\(ac|ad)\(\\s\\up"))
            {
                return $"<ruby>{InstrList[3]}<rt>{InstrList[1]}</rt></ruby>";
            }
            //ルビ以外は記号を除いて出力
            var sb = new StringBuilder();
            foreach (var txt in InstrList)
            {
                if (string.IsNullOrEmpty(txt))
                    continue;
                //foreach (var ch in new[] {"eq", ")", "(" })
                //{
                if (txt.IndexOf("eq") >= 0)
                    continue;
                if (txt.IndexOf("(") >= 0)
                    continue;
                if (txt.IndexOf(")") >= 0)
                    continue;
                sb.Append(txt);
                //}
            }
            return sb.ToString();
        }

        void SetFldChar(XmlNode w_run, out bool isTextOut)
        {
            isTextOut = false;
            if (!w_run.HasChildNodes) return;
            var fldChar = w_run.ChildNodes.Cast<XmlNode>()
                .FirstOrDefault(n => n.Name == "w:fldChar");
            if (fldChar?.Attributes == null) return;
            var fldValue = fldChar.Attributes["w:fldCharType"].Value;
            if (fldValue == "begin") IsFldChar = true;
            if (fldValue == "end")
            {
                IsFldChar = false;
                isTextOut = true;
            }
        }

        void SetCharStatus(XmlNode r_prop, ref List<WordXmlCharStatus> statusLst)
        {
            foreach (XmlNode prop in r_prop.ChildNodes)
            {
                if (prop.Name == "w:u")
                {
                    //sb.Append("<上線");
                    var value = prop.Attributes?["w:val"]?.Value;
                    if (!string.IsNullOrEmpty(value))
                    {
                        var status = new WordXmlCharStatus
                        {
                            AttrType = WordXmlCharStatus.EnumAttrType.Underline
                        };
                        switch (value)
                        {
                            case "dash":
                                status.AttrList.Add("種類", "破線");
                                break;
                            case "dotted":
                                status.AttrList.Add("種類", "点線");
                                break;
                            case "wave":
                                status.AttrList.Add("種類", "波線");
                                break;
                            case "double":
                                status.AttrList.Add("種類", "二重");
                                break;
                            case "single":
                            case "thick": //太線
                                break;
                            default:
                                Log.err(Path, 0, "underline", $"無効な下線種類 {value}");
                                break;
                        }
                        statusLst.Add(status);
                    }
                }
                if (prop.Name == "w:b")
                {
                    //sb.Append("<太字>");
                    statusLst.Add(new WordXmlCharStatus
                    {
                        AttrType = WordXmlCharStatus.EnumAttrType.Bold
                    });
                }
                if (prop.Name == "w:em")
                {
                    //sb.Append("<圏点>");
                    statusLst.Add(new WordXmlCharStatus
                    {
                        AttrType = WordXmlCharStatus.EnumAttrType.Kenten
                    });
                }
                if (prop.Name == "w:vertAlign")
                {
                    var value = prop.Attributes?["w:val"]?.Value;
                    if (!string.IsNullOrEmpty(value))
                    {
                        if (value == "subscript")
                        {
                            //sb.Append("<下付>");
                            statusLst.Add(new WordXmlCharStatus
                            {
                                AttrType = WordXmlCharStatus.EnumAttrType.Subscript
                            });
                        }
                        if (value == "superscript")
                        {
                            //sb.Append("<上付>");
                            statusLst.Add(new WordXmlCharStatus
                            {
                                AttrType = WordXmlCharStatus.EnumAttrType.Superscript
                            });
                        }
                    }
                }
            }
        }

        #region <w:r>

        public string ParseRun(XmlNode w_run)
        {
            var sb = new StringBuilder();
            //<w:rPr>...</w:rPr>の中にルビ等あり
            var r_prop = getFirstOfChilds(w_run, "w:rPr");
            var statusLst = new List<WordXmlCharStatus>();

            //フィールド文字列内かどうか endの時にまとめて出力
            SetFldChar(w_run, out bool isTextOut);
            if (isTextOut)
            {
                sb.Append(getInstrText());
                InstrList.Clear();
                return sb.ToString();
            }
            //フィールド文字列を取得 ==> 積み重ねて最後に出力
            //<w:r>...<w:rPr>...</w:rPr><w:instrText>６</w:instrText ></w:r>
            var instrText = getInstrTextFromWR(w_run);
            if (IsFldChar == true)
            {
                if (!string.IsNullOrEmpty(instrText))
                {
                    InstrList.Add(instrText);
                }
                return sb.ToString(); //結果空
            }

            if (r_prop != null)
            {
                SetCharStatus(r_prop, ref statusLst);
            }
            sb.Append(WordXmlCharStatus.GetText(statusLst, true));

            if (!string.IsNullOrEmpty(instrText))
                sb.Append("<ruby>");

            var w_text = getFirstOfChilds(w_run, "w:t");
            if (w_text != null)
            {
                foreach (XmlNode node in w_text.ChildNodes)
                {
                    var text = ParseTextElement(node);
                    sb.Append(text); //textとかrubyとか
                }
            }
            //注釈番号（＊１）  //本文側
            var w_endn = getFirstOfChilds(w_run, "w:endnoteReference"); //<w:endnoteReference w:id=""1""/>
            if (w_endn != null && w_endn.Attributes?["w:id"] != null)
            {
                var sujiZen = ZenHanUtil.ToZenkaku(w_endn.Attributes?["w:id"].Value);
                sb.Append($"{sujiZen}");
            }

            //注釈番号（＊１）  //注釈側 番号無し
            w_endn = getFirstOfChilds(w_run, "w:endnoteRef"); //<w:endnoteReference w:id=""1""/>
            if (w_endn != null)
            {
                var sujiZen = ZenHanUtil.ToZenkaku(ChushakuIndex.ToString());
                sb.Append($"{sujiZen}");
                ChushakuIndex++;
            }

            //ルビ
            var w_ruby = getFirstOfChilds(w_run, "w:ruby");
            if (w_ruby != null)
            {
                var w_rt = w_ruby
                    .Cast<XmlNode>()
                    .FirstOrDefault(n => n.Name == "w:rt");
                var ruby = w_rt
                    .Cast<XmlNode>()
                    .FirstOrDefault(n => n.Name == "w:r")
                    .Cast<XmlNode>()
                    .FirstOrDefault(n => n.Name == "w:t");
                var w_rubyBase = w_ruby
                    .Cast<XmlNode>()
                    .FirstOrDefault(n => n.Name == "w:rubyBase");
                var oya = w_rubyBase
                    .Cast<XmlNode>()
                    .FirstOrDefault(n => n.Name == "w:r")
                    .Cast<XmlNode>()
                    .FirstOrDefault(n => n.Name == "w:t");
                sb.Append(
                    $"<ruby>{ParseTextElement(oya.FirstChild)}<rt>{ParseTextElement(ruby.FirstChild)}</rt></ruby>");
            }

            if (!string.IsNullOrEmpty(instrText))
                sb.Append($"<rt>{instrText}</rt></ruby>");
            sb.Append(WordXmlCharStatus.GetText(statusLst, false));
            return sb.ToString();
        }

        #endregion

        #region Parse Xml Path

        void ParseDocumentXml(string documentPath)
        {
            using (var rd = XmlReader.Create(documentPath))
            {
                Path = documentPath;
                XmlDocument doc = new XmlDocument();
                doc.Load(rd);
                XmlNode root = doc.DocumentElement;

                var w_body = root.FirstChild; //w:body
                foreach (XmlNode node in w_body.ChildNodes)
                {
                    if (node.Name != "w:p") continue;
                    ParaList.Add(new WordXmlParaItem());
                    ParaList.Last().Text = ParseParagraph(node);
                }
            }
        }

        void ParseEndnotesXml(string endnotesPath)
        {
            using (var rd = XmlReader.Create(endnotesPath))
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(rd);
                XmlNode root = doc.DocumentElement; // <w:endnotes

                //var w_endnote = root.FirstChild;//<w:endnote
                foreach (XmlNode endnote in root.ChildNodes)
                {
                    foreach (XmlNode para in endnote.ChildNodes)
                    {
                        if (para.Name != "w:p") continue;
                        ParaList.Add(new WordXmlParaItem());
                        ParaList.Last().Text = ParseParagraph(para);
                    }
                }
            }
        }

        #endregion
    }
}
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using journal.console.lib.Models;
using kj.kihon;
using kj.kihon.Utils;

namespace journal.console.lib.Consoles
{
    public class StyleItem
    {
        public enum StyleType
        {
            ス字,
            スタ
        };
        public StyleType Style { get; set; }
        public string StyleName { get; set; }
    }
    
    public class journal06_util : kihon_base
    {
        private string OyaText { get; set; }
        private string RubyTextL { get; set; }  //左ルビ
        private string RubyTextR { get; set; }  //右ルビ
        Dictionary<string, bool> Flag = new Dictionary<string, bool>();
        private List<StyleItem> StyleList { get; set; }

        public journal06_util(ILogger log) : base(log)
        {
            Flag["ruby"] = false;
            Flag["rt"] = false;
            Flag["lt"] = false;
            StyleList = new List<StyleItem>();
        }

        public void Run(string jobdir)
        {
            jobdir.existDir();
            var txtdir = jobdir.combine("txt");
            txtdir.existDir();
            var kjpdir = jobdir.combine("kjp").createDirIfNotExist();

            foreach (var srcpath in txtdir.getFiles("*.txt"))
            {
                Path = srcpath;
                var outpath = kjpdir.combine(srcpath.getFileNameWithoutExtension() + ".kjp");
                RunFromPath(srcpath, outpath);
            }
        }

        StyleItem.StyleType GetStyleType(string buf)
        {
            var dict = new Dictionary<string, StyleItem.StyleType>
            {
                {"ス字", StyleItem.StyleType.ス字},
                {"スタ", StyleItem.StyleType.スタ},
            };
            if (dict.ContainsKey(buf))
                return dict[buf];
            throw new Exception($"1コラム目がス字orスタでない");
        }
        /**
         ス字	大字
        ス字	太字
        ス字	ゴシ
        スタ	見出1
        スタ	見出2
        スタ	見出3
         */
        void ReadStylePath()
        {
            var stylePath = Path
                .getDirectoryName()
                .getUpDir()
                .combine("style.txt");
            if (!File.Exists(stylePath))
                throw new Exception($"style.txtがない path={stylePath}");
            Console.WriteLine($"スタイルの読み込み");
            Console.WriteLine($"==>{stylePath}");
            
            var strlst = FileUtil.getTextListFromPath(stylePath, Encoding.UTF8);
            foreach (var gyo in strlst)
            {
                var tokens = gyo.Split('\t');
                if (tokens.Length >= 2)
                {
                    StyleList.Add(new StyleItem
                    {
                        Style = GetStyleType(tokens[0]),
                        StyleName = tokens[1]
                    });
                }
            }
        }

        public void RunFromPath(string srcpath, string outpath)
        {
            Path = srcpath;
            
            // style.txtの読み込み
            ReadStylePath();
            
            System.Console.WriteLine($"{srcpath}");
            System.Console.WriteLine($"==>{outpath}");
            FileUtil.writeTextToFile(CreateTextFromPath(srcpath), Encoding.UTF8, outpath);
        }

        // 先頭改行を削除
        string TrimLeftKaigyo(string buf)
        {
            char[] tbl = {'\r', '\n'};
            while (true)
            {
                if (buf.Length > 0 && Array.IndexOf(tbl, buf[0]) >= 0)
                {
                    buf = buf.Substring(1);
                    continue;
                }
                break;
            }
            return buf;
        }

        // <改行>LISTを作成 <改行>は最後に入る
        void CreateKaigyoList(string path, out List<ParaItem> paralst)
        {
            paralst = new List<ParaItem>();
            var util = new TagTextUtil(Log);
            var taglst = new TagList();
            util.parseTextFromPath(path, Encoding.UTF8,ref taglst,false);

            var sb = new StringBuilder();
            foreach (var tag in taglst)
            {
                // 先頭改行を削除
                sb.Append(TrimLeftKaigyo(tag.ToString()));
                if (tag.getName() == "改行")
                {
                    paralst.Add(new ParaItem {Gyo = tag.GyoNo, Text = sb.ToString()});
                    sb.Length = 0;
                }
            }
        }

        void SetJisageMondo(ParaItem para, ref int curJisage, ref int curMondo)
        {
            var taglst = TagTextUtil.parseText(para.Text);
            foreach (TagBase tag in taglst)
            {
                //<揃字>で字下はリセットしない
                //if (tag.getName() == "揃字") 
                //{
                //    para.IsJisoroe = true;
                //    curJisage = 0;
                //    curMondo = 0;
                //}
                if (tag.getName() == "字下")
                    curJisage = tag.getValue("").toInt(0);
                if (tag.getName() == "問答")
                    curMondo = tag.getValue("").toInt(0);
            }
            para.Jisage = curJisage;
            para.Mondo = curMondo;
        }

        private TagBase PreTag { get; set; }

        string CreateTextFromPath(string path)
        {
            Path = path;

            //改行ごとに処理する
            CreateKaigyoList(path, out List<ParaItem> paralst);

            return CreateTextFromParaList(paralst);
        }

        public string CreateTextFromParaList(List<ParaItem> paralst)
        {
            var sb = new StringBuilder();
            var curJisage = 0;
            var curMondo = 0;
            foreach (var para in paralst)
            {
                Gyono = para.Gyo;
                //<選択 ラベル名>を先に読み込み
                var taglst = TagTextUtil.parseText(para.Text);
                var label = taglst.FirstOrDefault(m => m.getName() == "選択");
                if (label != null)
                    sb.Append(label.ToString());

                //字下,問答を各段落に設定する
                SetJisageMondo(para, ref curJisage, ref curMondo);

                if (para.IsJisoroe != true)
                    sb.Append($"<字下 {para.Jisage}><問答 {para.Mondo}>");
                sb.Append(CreateTextFromPara(para));
                sb.Append("\r\n");
            }
            return sb.ToString()
                    .Replace("――", "<分禁>――</分禁>")
                    .Replace("</割>", "</割注>")
                    .Replace("<割>", "<割注>")
                ;
        }

        string CreateTextFromPara(ParaItem para)
        {
            var sb = new StringBuilder();
            var taglst = TagTextUtil.parseText(para.Text);
            foreach (var tag in taglst)
            {
                Gyono = para.Gyo;
                
                string[] mushi = {"字下", "問答", "選択"};
                if (Array.IndexOf(mushi, tag.getName()) >= 0)
                    continue;

                const string TAG_KASEN = "下線";
                const string TAG_RUBY = "ruby";
                if (tag.getName() == "上線")
                {
                    if (tag.isClose())
                        ((TagItem) tag).name_ = "/" + TAG_KASEN;
                    else
                        ((TagItem) tag).name_ = TAG_KASEN;
                }

                if (Array.IndexOf(new[] {TAG_KASEN, TAG_RUBY}, tag.getName()) >= 0)
                {
                    if (PreTag != null && PreTag.getName() == tag.getName() && PreTag.isOpen() == tag.isOpen())
                        Log.err(Path, Gyono, "parsetag",
                            $"タグの組み合わせがおかしい tag={tag.ToString()} isopen={tag.isOpen()} pre={PreTag.isOpen()}");
                }
                //文字列
                if (tag.isText())
                {
                    sb.Append(ToText(tag));
                    continue;
                }
                // ルビ
                sb.Append(SetRubyFlag(tag));

                //タグ
                sb.Append(ToTag(tag));
                if (tag.isTag())
                {
                    PreTag = tag;
                }
            }
            return sb.ToString();
        }

        // ルビのフラグをセット、</ruby>で出力
        private string SetRubyFlag(TagBase tag)
        {
            if (tag.getName() == "ruby" || tag.getName() == "添")
            {
                // </ruby>でルビを出力
                if (tag.isClose())
                {
                    if (string.IsNullOrEmpty(OyaText))
                        Log.err(Path, Gyono, "RUBYFLAG", $"<ruby>または<添>に親文字がない");

                    var res = "";
                    // どっちもあるときは圏点+左ルビ
                    if (!string.IsNullOrEmpty(RubyTextR) && !string.IsNullOrEmpty(RubyTextL))
                    {
                        res= $"<圏点 位置=左 種類=\"{RubyTextL}\"><ruby>{OyaText}<rt>{RubyTextR}</rt></ruby></圏点>";
                        ResetRubyFlag();
                        return res;
                    }
                    
                    // 通常ルビ
                    res= $"<ruby>{OyaText}";
                    if (!string.IsNullOrEmpty(RubyTextL))
                        res += $"<lt>{RubyTextL}</lt>";
                    if (!string.IsNullOrEmpty(RubyTextR))
                        res += $"<rt>{RubyTextR}</rt>";
                    res += $"</ruby>";

                    ResetRubyFlag();
                    return res;
                }
                // reset
                ResetRubyFlag();
                Flag["ruby"] = tag.isOpen();
            }
            if (tag.getName() == "rt")
            {
                Flag["rt"] = tag.isOpen();
            }
            if (tag.getName() == "GR")
            {
                Flag["rt"] = tag.isOpen();
            }
            if (tag.getName() == "lt")
            {
                Flag["lt"] = tag.isOpen();
            }
            return "";
        }

        private void ResetRubyFlag()
        {
            Flag["ruby"] = false;
            Flag["rt"] = false; //reset
            Flag["lt"] = false;
            OyaText = "";
            RubyTextL = "";
            RubyTextR = "";
        }

        string ToTag(TagBase tag)
        {
            // 無視 ルビとして処理
            string[] mushi = {"添","GR","ruby","rt","lt","見出"};
            if (Array.IndexOf(mushi, tag.getName()) >= 0)
                return "";
            
            //  そのまま出力
            string[] valid = { "改行","字揃","縦横","圏点","下線","スタ"};
            if (Array.IndexOf(valid, tag.getName()) >= 0)
                return tag.ToString();
            
            // <ス字>→<ス字 大字>  style.txtを読み込み
            var suta = StyleList.FirstOrDefault(m => m.StyleName==tag.getName());
            if (suta != null)
            {
                return　tag.isOpen() ? $"<{suta.Style} {suta.StyleName}>" :  $"</{suta.Style}>";
            }
            
            Log.err(Path,Gyono,"tagtext",$"無効なタグ {tag.ToString()}");
            return tag.ToString();
        }

        string ToText(TagBase tag)
        {
//            if (Flag["ruby"] > 0 && Flag["rt"] == 2)
//            {
//                Log.err(Path, tag.GyoNo, "journal06", $"<rt>または<GR>の文字列は無効 [{tag.ToString()}]");
//            }

            if (Flag["ruby"])
            {
                if (Flag["lt"])
                    RubyTextL = tag.ToString();
                if(Flag["rt"])
                    RubyTextR = tag.ToString();
                if (!Flag["lt"] && !Flag["rt"])
                    OyaText = tag.ToString();
                return "";
            }
            
            var sb = new StringBuilder();

            var txt = tag.ToString()
                .Replace("", "&#x3033;")
                .Replace("〱", "α")
                .Replace("α", "&#x3033;&#x3035;")
                .Replace("&#12349;", "ヽ")
                .Replace("", "&#x3035;")
                .Replace("", "&#x303b;")
                .Replace("β", "&#x303b;")
                .Replace("￥", "")
                .Replace("$", "＄")
                .Replace("＄", "&#x25e6;")
                .Replace(")", "）")
                .Replace("(", "（");
            //2桁全角を変換
            var reg = new Regex("＊([０-９]{2,3})");
            while (reg.IsMatch(txt))
            {
                var m = reg.Match(txt);
                txt = txt.Replace("＊" + RegexUtil.getGroup(m, 1),
                    "＊<ス字 ゴシ>" + ZenHanUtil.ToHankaku(RegexUtil.getGroup(m, 1)) + "</ス字>");
            }
            //1桁全角を変換
            reg = new Regex("＊([０-９]{1})");
            while (reg.IsMatch(txt))
            {
                var m = reg.Match(txt);
                txt = txt.Replace("＊" + RegexUtil.getGroup(m, 1), "＊<ス字 ゴシ>" + RegexUtil.getGroup(m, 1) + "</ス字>");
            }
            sb.Append(CharUtil.sjis2utf(txt)); //namespaceをUTFに変換
            return sb.ToString();
        }
    }
}
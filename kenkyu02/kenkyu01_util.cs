using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using kj.kihon;
using System.Text.RegularExpressions;
using kenkyu.lib.Model;
using kenkyu.lib;
using kj.kihon.Utils;
using kjlib.lib.Models;

namespace kenkyu.lib
{
    /**
    ・改行(\n)で改行
    ・\は改行しないでつなげる
        <S> スタイル

    */
    public class kenkyu01_util : kihon_base
    {
        public kenkyu01_util(ILogger log) : base(log)
        {
            IsGosi = false;
            IsRub = false;
            IsRub2 = false;
            IsMushi = false;
            IsUetuki = false;
            IsNakatuki = false;
            IsFuri = IsFuri2 = false;
            FuriText = Furi2Text = "";
        }

        int Mondo { get; set; }
        int Jisage { get; set; }
        bool IsUL { get; set; } //<S （）内>  ==> 無視
        bool IsMushi { get; set; } //<S （）内>  ==> 無視

        bool IsFuri { get; set; }
        AttrList FuriAttr = new AttrList();
        string FuriText { get; set; }
        bool IsFuri2 { get; set; }
        AttrList Furi2Attr = new AttrList();
        string Furi2Text { get; set; }

        bool IsUetuki { get; set; }
        bool IsNakatuki { get; set; }
        bool IsGosi { get; set; } //<S ゴシ>
        bool IsRub { get; set; } //<RUB>
        string rubtxt_ = ""; //親文字
        string rub2txt_ = ""; //ルビ
        bool IsRub2 { get; set; } //<RUB2>

        string outJisageMondo(int jisage, int mondo)
        {
            string res = "";
            if (jisage != Jisage) res += $"<字下 {jisage}>";
            if (mondo != Mondo) res += $"<問答 {mondo}>";
            Jisage = jisage;
            Mondo = mondo;
            return res;
        }

        /**
        文字列をまずは置き換える
        */
        public string replaceText(string buf)
        {
            buf = buf
                    .Replace("〳〵", "α")
                    .Replace("\\_t", "")
                    .Replace("\\", "")
                    .Replace(",うた=",
                        " うた=") //<M 歌 番号="1",うた="ことしよりかゝみとみゆるいけ水のちよへてすまむかけそゆかしき　<J size=10 posi=TOP>書本破損以他本書入之</J>">
                    .Replace(",数字=", " 数字=")
                    .Replace(",左側=", " 左側=") //<M 左○ 親文字="へ",左側="〇">
                    .Replace("\\\r\n", "") //\\は改行しないでつなげる
                    .Replace("\r\n", "<改行>\r\n")
                    .Replace("_c12123", "「")
                    .Replace("_c12124", "」")
                    .Replace("_c12125", "『")
                    .Replace("_c12126", "』")
                    .Replace("_c15394", "[&C15394]")
                    .Replace("<KG>", "<改行>\r\n")
                ;
            //ちょんちょんトル
            buf = replaceQuotTag(TagTextUtil.parseText(buf));

            buf = Regex.Replace(buf, "<問(.*?)>", m => $"<問答 {ZenHanUtil.ToHankaku(m.Groups[1].ToString())}>");
            buf = Regex.Replace(buf, "<字下(.*?)>", m => $"<字下 {ZenHanUtil.ToHankaku(m.Groups[1].ToString())}>");
            buf = buf
                    .Replace("<字下 2継続>", "<字下 2>")
                ;
            if (buf.IndexOf("_c") >= 0)
                Log.err(Path, Gyono, "rep_text", $"_cがあります [{buf}]");

            return buf;
        }

        /**

            */
        List<string> errlst = new List<string>();

        public class temp
        {
            public string tag { get; set; }
            public string value { get; set; }
        }

        string getTextForTag(string tags) //再起用
        {
            TagList taglst = TagTextUtil.parseText(tags);
            StringBuilder sb = new StringBuilder();
            foreach (TagBase tag in taglst)
                sb.Append(getTextForTag(tag));
            return sb.ToString();
        }

        string getTextForTag(TagBase tag)
        {
            //無視
            var mushi = new List<temp>
            {
                new temp {tag = "M", value = "１行アキ"},
                new temp {tag = "M", value = "マーカー"},
                new temp {tag = "M", value = "マーカー全角"},
                new temp {tag = "mi", value = ""},
                new temp {tag = "b2", value = ""},
                new temp {tag = "b4", value = ""},
            };
            foreach (var item in mushi)
            {
                if (item.tag == tag.getName() && item.value == tag.getValue("", true))
                    return "";
            }

            //<S>は使うのがあるので無視フラグ(IsMushi)で無視する
            mushi = new List<temp>
            {
                new temp {tag = "S", value = "（）内"},
                new temp {tag = "S", value = "（）内（）"},
                new temp {tag = "S", value = "１０Ｑ"},
                new temp {tag = "S", value = "ULBL"},
                new temp {tag = "S", value = "グループ"},
                new temp {tag = "J", value = "tmotf"},
            };
            ///無視フラグ
            foreach (var item in mushi)
            {
                if (item.tag == tag.getName() && item.value == tag.getValue("", true))
                {
                    IsMushi = true;
                    return "";
                }
            }

            //見出 字下、問答を０にする
            string[] mida0 = {"索引小見出し", "初句小見出し", "歌見出しのど用", "中見出し", "歌題見出しのど用", "小見出し"};
            if (mida0.Contains(tag.getName()) == true)
            {
                return outJisageMondo(0, 0);
            }
            if (new string[] {"歌題見出し"}.Contains(tag.getName()) == true)
            {
                return outJisageMondo(5, 0);
            }
            if (new string[] {"歌見出し"}.Contains(tag.getName()) == true)
            {
                return outJisageMondo(0, 2);
            }
            //<FURI jkr=CUR pos=TOP lsp=0 jst=QC>ひ<FURI2 size=6.5>ヒ</FURI></S>
            if (tag.getName() == "FURI")
            {
                string res = "";
                IsFuri = tag.isOpen();
                if (IsFuri == true)
                {
                    FuriAttr.Clear();
                    FuriAttr.AddRange(tag.Attrs);
                }
                if (IsFuri == false)
                {
                    res = getTextFuri();
                    IsFuri2 = false;
                }
                return res;
            }
            if (tag.getName() == "FURI2")
            {
                if (IsFuri == true)
                {
                    Furi2Attr.Clear();
                    Furi2Attr.AddRange(tag.Attrs);
                }
                IsFuri2 = true;
                return "";
            }

            if (tag.getName() == "INL")
            {
                return $"<外字 {tag.getValue("path", true)}>";
            }
            if (tag.getName() == "MOVE")
            {
                return $"<改行>";
            }
            if (tag.getName() == "J")
            {
                if (tag.getValue("posi", true).Length > 0)
                {
                    if (tag.getValue("posi", true) == "TOP")
                    {
                        IsUetuki = true;
                        return $"<上付>";
                    }
                    if (tag.getValue("posi", true) == "CENTER")
                    {
                        IsNakatuki = true;
                        return $"<中付>";
                    }
                    return ""; //それ以外は無視
                }
                if (tag.getValue("", true) == "") //<J>が閉じタグ
                {
                    if (IsUetuki == true)
                    {
                        IsUetuki = false;
                        return "</上付>";
                    }
                    if (IsNakatuki == true)
                    {
                        IsNakatuki = false;
                        return "</中付>";
                    }
                    return "";
                }
            }

            if (tag.getName() == "M" && tag.getValue("", true) == "脚注")
            {
                return $"<下付>＊{tag.getValue("数字", true)}{tag.getValue("字", true)}</下付>";
            }
            if (tag.getName() == "M" && tag.getValue("", true) == "字Ｓ")
            {
                return $"<字エ>";
            }

            if (tag.getName() == "WARI")
            {
                if (tag.getValue("par").Length > 0) return "<割注>";
                return "</割注>";
            }
            if (tag.getName() == "M" && tag.getValue("") == "左○")
            {
                if (tag.getValue("親文字").Length == 0)
                    Log.err(Path, Gyono, "get_text", $"親文字=がない [{tag.ToString()}]");
                if (tag.getValue("左側").Length == 0)
                    Log.err(Path, Gyono, "get_text", $"左側=がない [{tag.ToString()}]");
                string oya = getTextForTag(tag.getValue("親文字")); //再起
                string ruby = getTextForTag(tag.getValue("左側"));
                return $"<ruby>{oya}<lt>{ruby}</lt></ruby>";
            }
            //<M 歌 番号="1",うた="ことしよりかゝみとみゆるいけ水のちよへてすまむかけそゆかしき　<J size=10 posi=TOP>書本破損以他本書入之</J>">
            if (tag.getName() == "M" && tag.getValue("うた").Length > 0)
            {
                string bango = tag.getValue("番号", true);
                if (bango.Length == 0) throw new Exception("歌番号がない");
                string naiyo = tag.getValue("うた", true);
                return
                    $"　<太字>{bango}</太字>　{FileUtil.getTextFromTextList(replaceMorisawaTag(new List<string> {naiyo}))}<改行>\r\n"; //もう一度
            }
            if (tag.getName() == "M" && tag.getValue("", true) == "全角合成")
            {
                return $"［{tag.getValue("数字", true)}］";
            }

            //<S ゴシ> ==> <太字>
            if (tag.getName() == "S" && tag.getValue("") == "UL")
            {
                IsUL = true;
                return "<下線>"; //<上線><下線>要調査
            }
            if (tag.getName() == "S" && tag.getValue("") == "ゴシ")
            {
                IsGosi = true;
                return "<太字>";
            }
            if (tag.getName() == "UL2") //<上線><下線>要調査
            {
                return "<" + (tag.isClose() == true ? "/" : "") + "下線>";
            }
            if (tag.getName() == "S" && tag.getValue("", true) == "") //<S>が閉じタグ
            {
                if (IsMushi == true)
                {
                    IsMushi = false;
                    return "";
                }
                if (IsUL == true)
                {
                    IsUL = false;
                    return "</下線>";
                }
                if (IsGosi == true)
                {
                    IsGosi = false;
                    return "</太字>";
                }
                return "";
            }

            //研究Lタグ
            if (Array.IndexOf(new string[] {"窓左", "窓右", "頁", "問答", "字下", "改行"}, tag.getName()) >= 0)
            {
                //数字かチェック
                string[] tbl = {"字下", "問答"};
                if (Array.IndexOf(tbl, tag.getName()) >= 0)
                {
                    if (tag.getValue("").toInt(-1) < 0)
                    {
                        throw new Exception($"値が数値でない TAG={tag.ToString()}");
                    }
                    if (tag.getName() == "字下") Jisage = tag.getValue("", true).toInt(-1);
                    if (tag.getName() == "問答") Mondo = tag.getValue("", true).toInt(-1);
                }
                return tag.ToString();
            }

            if (tag.getName() == "RUB")
            {
                if (IsRub == false)
                {
                    IsRub = true;
                    return "";
                }
                string res = $"<ruby>{rubtxt_}<rt>{rub2txt_}</rt></ruby>";
                IsRub = false;
                IsRub2 = false;
                rubtxt_ = "";
                rub2txt_ = "";
                return res;
            }
            if (tag.getName() == "RUB2")
            {
                IsRub2 = true;
                return "";
            }
            if (tag.isText() == true)
            {
                return getText(tag);
            }

            Log.err(Path, tag.GyoNo, "waka06", $"無効なタグです {tag.ToString()}");
            return tag.ToString() + "<!--無効なタグ-->";
        }

        string replaceQuotTag(TagList taglst)
        {
            StringBuilder sb = new StringBuilder();
            foreach (TagBase tag in taglst)
            {
                if (isQuotTagName(tag.getName()) == true)
                {
                    ((TagItem) tag).name_ = ((TagItem) tag).name_.Substring(1, tag.getName().Length - 2);
                }
                sb.Append(tag.ToString());
            }
            return sb.ToString();
        }

        string getTextFuri()
        {
            //IsFuri=true  
            if (IsFuri2 != true)
            {
                if (FuriAttr["size"] == "9" && FuriAttr["pos"] == "TOP")
                    return $"<上付>{FuriText}</上付>";
                if (FuriAttr["size"] == "9" && FuriAttr["pos"] == "BOTTOM")
                    return $"<下付>{FuriText}</下付>";
            }
            if (IsFuri2 == true)
            {
                //割注
                if (FuriAttr["size"] == "9" && Furi2Attr["size"] == "")
                    return $"<割注>{FuriText}<項段>{Furi2Text}</割注>";
                //左ルビ
                if (Furi2Attr["size"] == "6.5")
                    return $"<ruby>{FuriText}<lt>{Furi2Text}</lt></ruby>";
            }
            Log.err(Path, Gyono, "furitext", "無効な<FURI>の組み合わせです");
            return $"{FuriText}{Furi2Text}";
        }

        //テキストから
        string getText(TagBase tag)
        {
            if (IsFuri == true)
            {
                if (IsFuri2 == true)
                    Furi2Text = tag.ToString();
                else
                    FuriText = tag.ToString();
                return ""; //<furi>は</furi>で出力
            }

            if (IsRub2 == true) //ルビ
            {
                rub2txt_ = tag.ToString();
                return "";
            }
            if (IsRub == true) ///ルビ親文字
            {
                rubtxt_ = tag.ToString();
                return "";
            }
            return tag.ToString();
        }

        bool isMushiTag(TagBase tag)
        {
            string[] mushi =
            {
                "WRFRM", "em", "HSR1", "HSR2", "FIL", "UL1",
                "組替え区切り", "標準", /*"FURI", "FURI2",*/ "MAKI", "表開始", "表行", "TAYO", "注",
                "組替え終了", "段抜きタイトル", "本文１４ＱＬＡ２８", "KP", "bk", "mr", "G"
            };
            if (tag.isTag() == true && Array.IndexOf(mushi, tag.getName()) >= 0) return true;
            return false;
        }

        bool isQuotTagName(string tagname)
        {
            if (tagname.Length >= 3)
            {
                if (tagname[0] == '\"' && tagname[tagname.Length - 1] == '\"') return true;
            }
            return false;
        }

        /**
        モリサワタグを置き換える
            */
        public List<string> replaceMorisawaTag(List<string> lst)
        {
            Jisage = 0;
            Mondo = 0;
            List<string> strlst = new List<string>();
            foreach (var item in lst.Select((v, i) => new {v, i}))
            {
                string cur = "";
                string buf = replaceText(item.v);

                //4字下げ,3字下げ,2字下げ
                foreach (int zenlen in Enumerable.Repeat(2, 4).Reverse()) //4字下げ→3字下げ→2字下げ
                {
                    if (buf.Length > zenlen && buf.Substring(0, zenlen) == new string('　', zenlen) &&
                        buf.Substring(0, zenlen + 1) != new string('　', zenlen + 1))
                    {
                        cur += outJisageMondo(0, zenlen);
                        break;
                    }
                }
                //文章 全角1+問答0
                if (buf.Length > 2 && buf.Substring(0, 1) == "　" && buf.Substring(0, 2) != "　　")
                {
                    cur += outJisageMondo(0, 0);
                }
                //文章
                if (buf.Length > 1 && buf.Substring(0, 1) != "　")
                {
                    cur += outJisageMondo(0, 0);
                }

                Gyono = item.i;
                int gyono = Gyono;
                TagList taglst = TagTextUtil.parseText(buf, ref gyono, false);
                foreach (TagBase tag in taglst)
                {
                    //無視タグ
                    if (isMushiTag(tag) == true) continue;

                    //文字を出力またはルビのフラグなど
                    cur += getTextForTag(tag);
                }
                strlst.Add(cur);
                cur = "";
            }
            return strlst;
        }

        List<string> replaceMisekechi(List<string> strlst)
        {
            List<string> reslst = new List<string>();
            //ミセケチを置き換え
            foreach (var item in strlst.Select((v, i) => new {v, i}))
            {
                string buf = strlst[item.i]
                    .Replace("<ruby>けさ<rt>そらは</rt></ruby>_c12107_c12107",
                        "<ruby>けさ<rt>そらは</rt><lt 変形=\"50\">ββ</lt></ruby>")
                    .Replace("<ruby>かり<rt>こゑ</rt></ruby>_c12107_c12107",
                        "<ruby>かり<rt>こゑ</rt><lt 変形=\"50\">ββ</lt></ruby>")
                    .Replace("</ruby>_c12107", "<lt>β</lt></ruby>");
                buf = Regex.Replace(buf, "([あ-んア-ンゝ])_c12107",
                    m => $"<ruby>{m.Groups[1].ToString()}<lt 変形=\"50\">β</lt></ruby>");
                if (buf.IndexOf("_c12107") >= 0)
                {
                    Log.err(Path, item.i + 1, "waka_xml", "ミセケチ(_c12107)が変換されずに残っている");
                }
                reslst.Add(buf);
            }
            return reslst;
        }

        /**

            */
        public void run(string srcdir)
        {
            srcdir.existDir();
            string[] lst = srcdir.getFiles("*.txt");
            string outdir = srcdir.getUpDir().combine(srcdir.getLastDirName() + "-out");
            outdir.createDirIfNotExist();

            foreach (var srcpath in lst)
            {
                Path = srcpath;
                //文字列をまずは置き換える
                //出力ファイルを切り替え .out
                Encoding srcenc = EncodingUtil.SJIS;
                var strlst = FileUtil.getTextListFromPath(Path, srcenc); //前回はUnicode 2016.11.20はSJIS

                //モリサワのルビや太字などを置換
                strlst = replaceMorisawaTag(strlst);
                strlst = replaceMisekechi(strlst);

                //改行を挿入
                StringBuilder sb = new StringBuilder();
                foreach (var item in strlst.Select((v, i) => new {v, i}))
                {
                    sb.Append($"{item.v}<改行>\r\n");
                }
                string outpath = outdir.combine(srcpath.getFileName());
                FileUtil.writeTextToFile(sb.ToString(), Encoding.Unicode, outpath);
                Console.WriteLine($"==>{outpath}");
            }
        }
    }
}
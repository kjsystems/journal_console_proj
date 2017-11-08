using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using journal.console.lib.Models;
using kj.kihon;

namespace journal.console.lib.Consoles
{
    public class journal12_util : kihon_base
    {
        public journal12_util(ILogger log) : base(log)
        {
        }

        string CurTxt = "";
        private bool IsIgnoreZone { get; set; }

        enum Shotai
        {
            Mincho,
            Gosi,
            Small, //<小字>
            Big, //<大字>
            UpRight, //<上付>  
            DownLeft, //<下付>  
            InValid
        };

        private Shotai CurShotai { get; set; } = Shotai.Mincho;

        void reset()
        {
            CurTxt = "";
            CurShotai = Shotai.Mincho;
        }

        void ReplaceBeforeTextFile(string srcpath, string outpath)
        {
            var lst = FileUtil.getTextListFromPath(srcpath, EncodingUtil.SJIS);
            for (int m = 0; m < lst.Count; m++)
            {
                lst[m] = lst[m]
                        .Replace("<ス字 Ｇ /><改行>", "<改行>")
                        .Replace("<字形 種類=aalt 番号=8 CID=10763>&#x20dd;</字形>", "●")
                        .Replace("<字類 種類=none>", "")
                        .Replace("<字類 種類=\"\">", "")
                        .Replace("<字形 種類=hwid CID=252 スタ=1>５</字形>", "５")
                        .Replace("<字形 種類=hwid CID=-1 スタ=1>１</字形>", "１")
                        .Replace("<字形 種類=hwid CID=-1 スタ=1>２</字形>", "２")
                        .Replace("<字形 種類=hwid CID=-1 スタ=1>３</字形>", "３")
                        .Replace("<字形 種類=hwid CID=-1 スタ=1>４</字形>", "４")
                        .Replace("<字形 種類=hwid CID=-1 スタ=1>６</字形>", "６")
                        .Replace("<字形 種類=hwid CID=-1 スタ=1>８</字形>", "８")
                        .Replace("<字形 種類=hwid CID=-1 スタ=1>９</字形>", "９")
                        .Replace("<字形 種類=aalt 番号=5 CID=-1>「</字形>", "「")
                        .Replace("<字形 種類=aalt 番号=5 CID=-1>」</字形>", "」")
                        .Replace("<字形 種類=hwid CID=249 スタ=1>2</字形>", "2")
                        .Replace("<字形 種類=hwid CID=253 スタ=1>6</字形>", "6")
                        .Replace("<字形 種類=hwid CID=256 スタ=1>9</字形>", "9")
                        .Replace("<字形 種類=hwid CID=250 スタ=1>3</字形>", "3")
                        .Replace("<字形 種類=hwid CID=254 スタ=1>7</字形>", "7")
                        .Replace("<字形 種類=hwid CID=248 スタ=1>1</字形>", "1")
                        .Replace("<字形 種類=hwid CID=251 スタ=1>4</字形>", "4")
                        .Replace("<字形 種類=hwid CID=255 スタ=1>8</字形>", "8")
                        .Replace("<字形 種類=hwid CID=252 スタ=1>5</字形>", "5")
                        .Replace("<字形 種類=hwid CID=247 スタ=1>0</字形>", "0")
                        .Replace("<字形 種類=hwid CID=-1 スタ=1>＊</字形>", "＊")
                        .Replace("<字形 種類=vrt2 CID=-1>─</字形>", "─")
                        .Replace("<字形 種類=hwid CID=244 スタ=1>-</字形>", "-")
                        .Replace("<字形 種類=nalt CID=10078>ア</字形>", "ア")
                        .Replace("<字形 種類=nalt CID=10079>イ</字形>", "イ")
                        .Replace("<3033>", "u+3033")
                        .Replace("<3035>", "u+3035")
                        .Replace("<rt>ヒ</rt>", "<lt>β</lt>")
                        .Replace("<ス字 50％左ツキ /><ル定 親間=1-2-1アキ><ル定 かけ=ルビ１文字分><!--UCSで出力-->&#x3193;<ス字 [なし] />",
                            "<下付>二</下付>")
                        .Replace("<ス字 50％左ツキ /><字送 92%><字空 前=八分>一<字送 100%><字空 前=自動>レ<ス字 [なし] />", "<下付>一レ</下付>")
                        .Replace("<ス字 50％左ツキ />レ<ス字 [なし] />", "<下付>レ</下付>")
                        .Replace("<書体 ##太890856賀茂真淵_151029_01>[&C0]", "簡")
                        .Replace("<書体 ##太890856賀茂真淵_151029_01><行送 22q>[&C0]", "簡")
                        .Replace("にかくれて出られ</rt></ruby></圏点>", "にかくれて出られ</rt></圏点></ruby>")
                        .Replace("我あらばこそ<文字 6.5q><圏点 種類=ヽ><割注 ><割定 行数=3>", "我あらばこそ<文字 6.5q><割注 ><割定 行数=3><圏点 種類=ヽ>")
                    ;
            }
            FileUtil.writeTextToFile(lst, EncodingUtil.SJIS, outpath);
        }


        string ReplaceBold(string buf)
        {
            var regex = new Regex(@"〔(.*?)〕");
            while (regex.IsMatch(buf))
            {
                var match = regex.Match(buf);
                buf = regex.Replace(buf, "<太字>$STT$" + match.Groups[1].ToString() + "$END$</太字>", 1);
            }
            return buf
                .Replace("$STT$", "〔")
                .Replace("$END$", "〕");
        }

        // ス字
        string toStringSuji(TagBase tag)
        {
            //ゴシ
            string[] gosi = {"歌番号", "注釈タイトル", "本文ゴシック"};
            if (Array.IndexOf(gosi, tag.getValue("")) >= 0)
            {
                return toStringShotai(Shotai.Gosi);
            }
            //明朝
            string[] mincho =
            {
                "本文", "小見出しサブ", "小見出しのサブ", "[なし]",
            };
            if (Array.IndexOf(mincho, tag.getValue("")) >= 0)
            {
                return toStringShotai(Shotai.Mincho);
            }
            if (tag.getValue("") == "Ｇ")
            {
                return toStringShotai(Shotai.Gosi);
            }
            if (tag.getValue("") == "注釈本文")
            {
                return toStringShotai(Shotai.Small);
            }
            if (tag.getValue("") == "小見出し")
            {
                return toStringShotai(Shotai.Big);
            }
            if (tag.getValue("") == "小_右寄せ")
            {
                return toStringShotai(Shotai.UpRight);
            }
            if (tag.getValue("") == "返り点")
            {
                return toStringShotai(Shotai.DownLeft);
            }
            Log.err(Path, Gyono, "tostring", $"未登録の属性 {tag.ToString()}");
            return tag.ToString();
        }

        string toStringShotai(TagBase tag)
        {
            //現在の書体から明朝/ゴシを取得
            var shotai = getShotai(tag.getValue(""));
            if (shotai == Shotai.InValid)
            {
                Log.err(Path, Gyono, "tostring", $"明朝・ゴシを取得できない [{tag.ToString()}]");
                return "";
            }
            //現在の書体と異なっていれば出力
            return toStringShotai(shotai);
        }

        //現在の書体と異なっていれば出力
        string toStringShotai(Shotai shotai, bool isCloseOnly = false /*閉じタグのみ出力*/)
        {
            var tbl = new Dictionary<Shotai, Pair<string, bool>>
            {
                {Shotai.Gosi, new Pair<string, bool>("太字", true)},
                {Shotai.Small, new Pair<string, bool>("小字", true)},
                {Shotai.Big, new Pair<string, bool>("大字", true)},
                {Shotai.UpRight, new Pair<string, bool>("上付", true)},
                {Shotai.DownLeft, new Pair<string, bool>("下付", true)},
            };
            //閉じタグのみ出力
            if (isCloseOnly)
            {
                //1.閉じタグがあれば閉じる CurShotai...直前の値
                if (tbl.ContainsKey(CurShotai) && tbl[CurShotai].Two == true)
                {
                    return $"</{tbl[CurShotai].One}>";
                }
                return "";
            }

            if (CurShotai != shotai)
            {
                var buf = "";
                //1.閉じタグがあれば閉じる CurShotai...直前の値
                if (tbl.ContainsKey(CurShotai) && tbl[CurShotai].Two == true)
                {
                    buf += $"</{tbl[CurShotai].One}>";
                }

                //2.値を出力する
                CurShotai = shotai;
                if (tbl.ContainsKey(shotai))
                    buf += $"<{tbl[shotai].One}>";
                return buf;
            }
            return "";
        }

        Shotai getShotai(string value)
        {
            string[] mincho =
            {
                "A-OTF リュウミン Pro L-KL", "A-OTF リュウミン Pro H-KL", "225 L-KL",
                "A-OTF リュウミン Pro EB-KL", "222 L-KL", "Heiti TC Light",
                "はんなり明朝", "A-OTF 正楷書CB1 Std", "223 L-KL",
                "小塚明朝 Pro R",
                "##太890856賀茂真淵_151029_01", "216 L-KL", "FOTC-ARBaosong2GB Light", "218 L-KL",
            };
            string[] gosi =
            {
                "ゴシック", "A-OTF ゴシックMB101 Pro DB",
                "A-OTF ゴシックMB101 Pro L",
                "A-OTF 中ゴシックBBB Pr6 Medium"
            };
            if (Array.IndexOf(mincho, value) >= 0)
                return Shotai.Mincho;
            if (Array.IndexOf(gosi, value) >= 0)
                return Shotai.Gosi;
            return Shotai.InValid;
        }

        string toString(TagBase tag)
        {
            if (tag.isOpen())
            {
                switch (tag.getName())
                {
                    case "項段":
                        return "<項段/>";
                    case "圏点":
                        var sb = new StringBuilder();
                        if (IsMisekechi == true)
                        {
                            sb.Append("<!-- ミセケチ内の圏点 -->");
                        }
                        if (tag.isOpen() && !string.IsNullOrEmpty(tag.getValue("種類")))
                        {
                            sb.Append($"<圏点 種類=\"{tag.getValue("種類")}\">");
                            return sb.ToString();
                        }
                        sb.Append(tag.ToString());
                        return sb.ToString();
                    case "下線": //<下線 "%%%ITextAttrUnderlineMode=(enum) 0"> <下線 "%%%ITextAttrUnderlineMode=(enum) 1">
                        return (!string.IsNullOrEmpty(tag.Attrs[0].Value) && tag.Attrs[0].Value == "(enum) 1\"")
                            ? "<下線>"
                            : $"</下線>";
                    case "書体":
                        //明朝、ゴシを切り替え
                        return toStringShotai(tag);
                    case "ス字":
                        return toStringSuji(tag);
                    case "揃字":
                        if (tag.getValue("") != "右")
                        {
                            Log.err(Path, tag.GyoNo, "tostring", $"無効な<揃字>の値 {tag.ToString()}");
                            return "";
                        }
                        return tag.ToString(); //<ス字 右> or <字Ｓ>
                }
            }
            if (tag.Attrs != null && tag.Attrs.Count > 0)
            {
                var v = tag.Attrs[0].Value;
                if (!string.IsNullOrEmpty(v) && v[0] != '"')
                {
                    tag.Attrs[0].Value = $"\"{v}\"";
                }
            }
            return tag.ToString();
        }

        void outCurrentPage(TagBase tag)
        {
            var curPage = tag.getValue("").toInt(-1);
            CurTxt += $"<頁 {curPage}>";
        }

        void AddPara(List<ParaItem> paralst)
        {
            paralst.Add(new ParaItem
            {
                Text = CurTxt,
            });
            reset();
        }

        void procIgnoreZone(TagBase tag)
        {
            IsIgnoreZone = tag.isOpen();
        }

        void procTab(TagBase tag)
        {
            CurTxt += "　";
        }

        private bool IsMisekechi { get; set; }

        void ParseText(TagList taglst, ref List<ParaItem> paralst)
        {
            string[] mushi =
            {
                "ＸＹ", "縦線", "四角", "頁始", "圏定", "多線", "集合", "字送",
                "行送", "字送", "基線", "横線", "行取", "タ定", "文字", "長体", "ル定",
                "平体", "組み", "禁則", "楕円", /*"スタ",*/
                "四分", "三分", "段禁", "字色", "字空", "字間",
                "字上", "連係", "字類", "割定", "字下", "スタ"
            };
            //タグ名
            string[] mushi2 =
            {
                "ス字+見出し名前", "ス字+大見出し", "ス字+見出し名前ルビ",
                "スタ+[基本段落]", "スタ+[段落スタイルなし]", "揃字+左", "揃字+中", "揃字+揃左",
                "ス字+小2列", "ス字+ノンブル",
                "ス字+縦　全幅ハイフン",
                "ス字+標準字形", "ス字+脚注参照番号", "ス字+ルビ８Ｑ特中", "ス字+ルビ８Ｑ特",
                "ス字+12Ｑ", "ス字+11Q（字オ10.75）", "ス字+6.5Q右ツキ", "ス字+ルビ８Ｑ特（JIS）",
                "ス字+13Ｑ", "ス字+ベタ", "ス字+圏点", "ス字+ミセケチ＿ヒ", "ス字+ミセケチ＿ヒ（u+3033u+3035に付与）",
                "ス字+簡体字"
            };

            string[] valid =
            {
                "ruby", "rt", "lt", "縦横", "下線", "字下", "圏点", "書体",
                "ス字", "一下", "揃字", "割注", "項段", "下付"
            };
            reset();

            var tbl = new Dictionary<string, Action<TagBase>>()
            {
                {"PAGE", outCurrentPage},
                {"table", procIgnoreZone},
                {"集合", procIgnoreZone},
                {"タブ", procTab},
            };

            //<縦横>は改行前で閉じる
            bool isTateyoko = false;
            foreach (var tag in taglst)
            {
                Gyono = tag.GyoNo;
                if (tag.getName() == "縦横")
                {
                    isTateyoko = tag.isOpen();
                }

                //タグを処理（Actionで渡している）
                if (tbl.ContainsKey(tag.getName()))
                {
                    tbl[tag.getName()](tag);
                    continue;
                }
                if (IsIgnoreZone) continue;

                if (tag.getName() == "ス字")
                {
                    if (tag.getValue("") == "ミセケチ＿ヒ" || tag.getValue("") == "ミセケチ＿ヒ（u+3033u+3035に付与）")
                        IsMisekechi = true;
                    if (tag.getValue("") == "[なし]")
                        IsMisekechi = false;
                }


                //無視タグ
                if (tag.isTag() && Array.IndexOf(mushi, tag.getName()) >= 0)
                    continue;
                //無視タグ+値
                if (tag.isTag() && Array.IndexOf(mushi2, $"{tag.getName()}+{tag.getValue("")}") >= 0)
                    continue;
                if (tag.isComment())
                    continue;

                //有効タグ
                if (tag.isTag() && Array.IndexOf(valid, tag.getName()) >= 0)
                {
                    CurTxt += toString(tag);
                    continue;
                }
                if (tag.getName() == "改行")
                {
                    if (isTateyoko)
                    {
                        CurTxt += "</縦横>";
                    }
                    AddPara(paralst);
                    if (isTateyoko)
                    {
                        CurTxt += "<縦横>";
                        isTateyoko = true;
                    }
                    continue;
                }
                if (tag.isTag())
                {
                    Log.err(Path, tag.GyoNo, "parsettxt", $"無効なタグ {tag.ToString()}");
                    continue;
                }
                if (tag.isText() && tag.ToString().delKaigyo().Length > 0)
                {
                    CurTxt += tag.ToString().delKaigyo();
                }
            }
        }

        string ReplaceAfter(string buf)
        {
            return buf
                    .Replace("<縦横></縦横>", "")
                    .Replace("&#x3033;&#x3035;", "α")
                    .Replace("<ruby>●<rt>にイ（朱）</rt></ruby>", "<ruby><小字>〇</小字><rt>にイ（朱）</rt></ruby>")
                    .Replace("<ruby>●<rt>京には</rt></ruby>", "<ruby><小字>〇</小字><rt>京には</rt></ruby>")
                    .Replace("<ruby>●<rt>直</rt></ruby>", "<ruby><小字>〇</小字><rt>直</rt></ruby>")
                    .Replace("<ruby>す<rt>ぬ（朱）</rt></ruby><ruby>●<lt>β</lt></ruby>",
                        "<ruby>す<rt>ぬ（朱）</rt><lt>β</lt></ruby>")
                    .Replace("<ruby>●<rt>世</rt></ruby>", "<ruby><小字>〇</小字><rt>世</rt></ruby>")
                    .Replace("<ruby>●<rt>の（朱）にイ（朱）</rt></ruby>", "<ruby><小字>〇</小字><rt>の（朱）にイ（朱）</rt></ruby>")
                    .Replace("<ruby>●<rt>右（朱）</rt></ruby>", "<ruby><小字>〇</小字><rt>右（朱）</rt></ruby>")
                    .Replace("<ruby>●<rt>雲</rt></ruby>", "<ruby><小字>〇</小字><rt>雲</rt></ruby>")
                    .Replace("<ruby>●<rt>と</rt></ruby>", "<ruby><小字>〇</小字><rt>と</rt></ruby>")
                    .Replace("<ruby>露<rt>靍</rt></ruby><ruby>●<lt>β</lt></ruby>",
                        "<ruby>露<rt>靍</rt><lt>β</lt></ruby>")
                    .Replace("<ruby 属性=\"複数\">なく<rt>しら</rt></ruby><ruby>●<lt>β</lt></ruby><ruby>●<lt>β</lt></ruby>",
                        "<ruby 属性=\"複数\">なく<rt>しら</rt><lt>ββ</lt></ruby>")
                    .Replace("<ruby 属性=\"GROUP\">●か<rt>朝臣イ（朱）</rt></ruby>",
                        "<ruby 属性=\"GROUP\"><小字>〇</小字>か<rt>朝臣イ（朱）</rt></ruby>")
                    .Replace("<ruby>●<rt>是は</rt></ruby>", "<ruby><小字>〇</小字><rt>是は</rt></ruby>")
                    .Replace("<ruby>●<rt>なる</rt></ruby>", "<ruby><小字>〇</小字><rt>なる</rt></ruby>")
                    .Replace("<ruby>●<rt>く</rt></ruby>", "<ruby><小字>〇</小字><rt>く</rt></ruby>")
                    .Replace("<ruby>●<rt>の（朱）</rt></ruby>", "<ruby><小字>〇</小字><rt>の（朱）</rt></ruby>")
                    .Replace("<ruby>●<rt>執行</rt></ruby>", "<ruby><小字>〇</小字><rt>執行</rt></ruby>")
                    .Replace("<ruby>●<rt>せさ（朱）</rt></ruby>", "<ruby><小字>〇</小字><rt>せさ（朱）</rt></ruby>")
                    .Replace("<ruby>●<rt>野</rt></ruby>", "<ruby><小字>〇</小字><rt>野</rt></ruby>")
                    .Replace("<ruby>●<rt>を（朱）</rt></ruby>", "<ruby><小字>〇</小字><rt>を（朱）</rt></ruby>")
                    .Replace("<ruby>●<rt>したるイ（朱）</rt></ruby>", "<ruby><小字>〇</小字><rt>したるイ（朱）</rt></ruby>")
                    .Replace("<ruby 属性=\"GROUP\">●●●<rt>よめるイ（朱）</rt></ruby>",
                        "<ruby 属性=\"GROUP\"><小字>〇〇〇</小字><rt>よめるイ（朱）</rt></ruby>")
                    .Replace("<ruby>●<rt>て（朱）</rt></ruby>", "<ruby><小字>〇</小字><rt>て（朱）</rt></ruby>")
                    .Replace("<ruby>●<rt>（ら）</rt></ruby>", "<ruby><小字>〇</小字><rt>（ら）</rt></ruby>")
                    .Replace("<ruby>●<rt>（一字分アキ）</rt></ruby>", "<ruby><小字>〇</小字><rt>（一字分アキ）</rt></ruby>")
                    .Replace("<ruby>●<rt>へいらんイ（朱）</rt></ruby>", "<ruby><小字>〇</小字><rt>へいらんイ（朱）</rt></ruby>")
                    .Replace("<ruby>●<rt>政イ（朱）</rt></ruby>", "<ruby><小字>〇</小字><rt>政イ（朱）</rt></ruby>")
                    .Replace("<ruby 属性=\"GROUP\">●●侍●し●に<rt>",
                        "<ruby 属性=\"GROUP\"><小字>〇</小字><小字>〇</小字>侍<小字>〇</小字>し<小字>〇</小字>に<rt>")
                    .Replace("<ruby>●<rt>な（朱）</rt></ruby>", "<ruby><小字>〇</小字><rt>な（朱）</rt></ruby>")
                    .Replace("<ruby>●<rt>のイ（朱）</rt></ruby>", "<ruby><小字>〇</小字><rt>のイ（朱）</rt></ruby>")
                    .Replace("<ruby>●<rt>に</rt></ruby>", "<ruby><小字>〇</小字><rt>に</rt></ruby>")
                    .Replace("<ruby>●<rt>日イ（朱）</rt></ruby>", "<ruby><小字>〇</小字><rt>日イ（朱）</rt></ruby>")
                    .Replace("<ruby>●<rt>と（朱）</rt></ruby>", "<ruby><小字>〇</小字><rt>と（朱）</rt></ruby>")
                    .Replace("<ruby>返<rt>外</rt></ruby><ruby>●<lt>β</lt></ruby>",
                        "<ruby>返<rt>外</rt><lt>β</lt></ruby>")
                    .Replace("<ruby>事<rt>題</rt></ruby><ruby>●<lt>β</lt></ruby>",
                        "<ruby>事<rt>題</rt><lt>β</lt></ruby>")
                    .Replace("<ruby>願<rt>甲斐</rt></ruby><ruby>●<lt>β</lt></ruby>",
                        "<ruby>願<rt>甲斐</rt><lt>β</lt></ruby>")
                ;
        }

        public void Run(string jobdir)
        {
            jobdir.existDir();
            var srcdir = jobdir.combine("kjp");
            var outdir = jobdir.combine("txt");
            var outTxtDir = jobdir.combine("txt-out").createDirIfNotExist();
            srcdir.existDir();
            outdir.createDirIfNotExist();

            var srclst = srcdir.getFiles("*.kjp");
            foreach (var kjppath in srclst)
            {
                Console.WriteLine($"{kjppath}");
                // 置き換え
                var kjpOutdir = jobdir.combine("kjp-out").createDirIfNotExist();
                var outFileName = kjppath.getFileName();
                var kjpOutPath = kjpOutdir.combine(outFileName);
                ReplaceBeforeTextFile(kjppath, kjpOutPath);
                Console.WriteLine($"==>{kjpOutPath}");

                Path = kjpOutPath;
                //1.KJPを読み込み
                var taglst = new TagList();
                var util = new TagTextUtil(Log);
                util.parseTextFromPath(kjpOutPath, EncodingUtil.SJIS, ref taglst);

                //2.KJPをParse
                var paralst = new List<ParaItem>();
                ParseText(taglst, ref paralst);

                //3.TXTを出力
                var txtpath = outTxtDir.combine(kjppath.getFileNameWithoutExtension() + ".txt");
                Console.WriteLine($"==>{txtpath}");

                var buf = ReplaceAfter(ParaItem.CreateText(paralst));
                // 太字
                buf = ReplaceBold(buf);

                FileUtil.writeTextToFile(buf, Encoding.UTF8, txtpath);
            }
            //全部つなげる
            var sb = new StringBuilder();
            foreach (var txtpath in outTxtDir.getFiles("*.txt"))
            {
                sb.AppendLine($"<!-- {txtpath.getFileName()} -->");
                sb.Append(FileUtil.getTextFromPath(txtpath, Encoding.UTF8));
            }
            var outpath = outdir.combine($"006.txt");
            Console.WriteLine($"==>{outpath}");
            FileUtil.writeTextToFile(sb.ToString(), Encoding.UTF8, outpath);
        }
    }
}
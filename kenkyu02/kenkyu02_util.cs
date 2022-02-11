using kj.kihon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using kenkyu.lib.Model;
using kjlib.lib.Models;

namespace kenkyu.lib
{
    public class kenkyu02_util : kihon_base
    {
        public kenkyu02_util(ILogger log) : base(log)
        {
        }

        const string FNAME_LIST = "収載一覧.txt";
        const string FLD_BANGO = "研究書ライブラリー番号";
        const string FLD_MOTO = "元ファイル";
        const string FLD_TITLE = "書籍タイトル";
        const string FLD_CHOSHA = "著者";
        const string BR = "\r\n";
        const string MADO_MIGI = "窓右";
        const string MADO_HIDARI = "窓左";

        //const string InitDataDir = @"C:\git\waka_proj\kenkyu.web\kenkyu.web\InitData\";
        /**
            本文XML(<窓一覧>)ではなくて入稿テキスト用 <窓右><窓左>
            */
        public List<MadoItem> getMadoList(List<string> strlst)
        {
            List<MadoItem> lst = new List<MadoItem>();
            foreach (var item in strlst.Select((v, i) => new {v, i}))
            {
                var mado = "";
                var bMado = false;
                int gyono = item.i + 1;
                var taglst = TagTextUtil.parseText(item.v, ref gyono, false);
                foreach (TagBase tag in taglst)
                {
                    string[] tbl = {MADO_MIGI, MADO_HIDARI};
                    if (tag.isOpen() == true && Array.IndexOf(tbl, tag.getName()) >= 0)
                    {
                        bMado = true;
                        mado = tag.getName();
                    }
                    if (tag.isOpen() != true && Array.IndexOf(tbl, tag.getName()) >= 0)
                    {
                        bMado = false;
                    }
                    //窓の文字列を取得
                    if (tag.isText() == true && bMado == true)
                    {
                        lst.Add(new MadoItem()
                        {
                            IsLeft = (mado == MADO_HIDARI),
                            IsRight = (mado == MADO_MIGI),
                            text = tag.ToString(),
                            href = item.i + 1
                        });
                        continue;
                    }
                    if (tag.getName() == "名前")
                    {
                        var name = lst.FirstOrDefault(m => m.text == tag.getValue(""));
                        if (name == null)
                        {
                            Log.err(Path, item.i + 1, "madolist", $"<名前>で指定した窓がない name={tag.getValue("")}");
                            continue;
                        }
                        name.href = item.i + 1; //リンク先を設定する
                    }
                }
            }
            if (lst.Count == 0)
            {
                Log.err(Path, 0, "madolist", "<窓右><窓左>を1つも取得できない");
            }

            return lst;
        }

        // madolstはSQL出力用に細切れにしてる
        string createMokujiSqlText(ShomeiData s, string tblname, ref int id, List<MadoItem> madolst)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var item in madolst.Select((v, i) => new {v, i}))
            {
                MadoItem mado = item.v;
                //sb.Append($"({id++},'{mado.text}',{s.bango},0,{mado.level}, {mado.href}, NOW())");
                sb.Append($"('{mado.text}',{s.Bango},0,{mado.level}, {mado.href}, NOW())");
                if (madolst.Last() == mado) break;
                sb.Append("," + BR); //最後以外出力
            }
            sb.Append(";" + BR + BR);
            return
                $"#目次一覧 書名={s.Shomei}" + BR
                + $"DELETE FROM `{tblname}` WHERE `article_id`={s.Bango};{BR}" //
                + $"INSERT INTO {tblname} (`title`,`article_id`,`parent_id`,`level`,`href`,`created`) VALUES" + BR
                + sb.ToString();
        }

        /**
        Excelから書名一覧を取得する
            */
        public List<ShomeiData> readShomeiList(string srcdir)
        {
            List<ShomeiData> lst = new List<ShomeiData>();
            var srcpath = srcdir.combine(FNAME_LIST);
            try
            {
                srcpath.existFile();
                CSVFileReader rd = new CSVFileReader(Log);
                rd.setupCommentToken('*');
                rd.setupTargetToken('\t');
                CSVData csv = rd.readFile(srcpath, Encoding.UTF8, true /*フィールド名を使用する*/);
                foreach (var item in csv.Rows.Select((v, i) => new {v, i}))
                {
                    lst.Add(new ShomeiData()
                    {
                        Bango = csv.Rows[item.i].getCell(csv.Fields, FLD_BANGO).toInt(-1),
                        Shomei = csv.Rows[item.i].getCell(csv.Fields, FLD_TITLE),
                        Chosha = csv.Rows[item.i].getCell(csv.Fields, FLD_CHOSHA),
                        FileName = csv.Rows[item.i].getCell(csv.Fields, FLD_MOTO),
                    });
                }
                if (lst.Count == 0) throw new Exception("収載一覧.txtから値を取得できない(UTF8/タブ区切り)");
            }
            catch (Exception ex)
            {
                Log.err(srcpath, 0, "readshome", ex.Message);
            }
            return lst;
        }

        /**
        書名一覧のSQLを作成
            */
        string createShomeiSqlText(List<ShomeiData> shomeilst)
        {
            string tblname = "articles";
            List<string> lst = new List<string>();
            StringBuilder sb = new StringBuilder();

            foreach (var item in shomeilst.Select((v, i) => new {v, i}))
            {
                ShomeiData s = item.v;
                //sb.Append($"({s.bango},'{s.shomei}','{s.chosha}',NOW())");
                sb.Append($"({s.Bango},'{s.Shomei}','{s.Chosha}',NOW())");
                if (shomeilst.Last() == s) break;
                sb.Append("," + BR);
            }
            sb.Append(";" + BR + BR);
            return
                $"#書名一覧" + BR
                + $"INSERT INTO {tblname} (`id`,`title`,`chosha`,`created`) VALUES" + BR
                + sb.ToString();
        }

        public List<MadoItem> createMokujiList(ShomeiData shomei, string xmldir)
        {
            StringBuilder sb = new StringBuilder();
            //string Path = tmpdir.combine($"{shomei.bango}.txt");    //TMPファイル -2.txt		※.xmlだと<窓右>はもうないので
            Path = xmldir.combine(shomei.FileName); //TMPファイル -2.txt		※.xmlだと<窓右>はもうないので

            //XMLから窓左,窓右を読み込み
            FileUtil fu = new FileUtil(Log);
            List<string> strlst = fu.CreateKaigyoListFromPath(Path, Encoding.UTF8);
            kenkyu01_util util = new kenkyu01_util(Log);
            List<MadoItem> madolst = getMadoList(strlst);
            return madolst;
        }

        /**
    
                */
        string createMokujiSqlText(List<ShomeiData> shomeilst, string xmldir)
        {
            StringBuilder sb = new StringBuilder();
            int id = 1;

            string tblname = "contents";
            /*sb.Append($"TRUNCATE TABLE `{tblname}`;{BR}");*/ //消さない
            foreach (var item in shomeilst.Select((v, i) => new {v, i}))
            {
                ShomeiData s = item.v;
                Path = xmldir.combine(s.FileName); //TMPファイル -2.txt		※.xmlだと<窓右>はもうないので

                //XMLから窓左,窓右を読み込み
                FileUtil fu = new FileUtil(Log);
                List<string> strlst = fu.CreateKaigyoListFromPath(Path, Encoding.UTF8);
                kenkyu01_util util = new kenkyu01_util(Log);
                List<MadoItem> madolst = getMadoList(strlst);

                const int maxrow = 50;
                for (int m = 0; m < madolst.Count; m += maxrow)
                {
                    List<MadoItem> lst = new List<MadoItem>();
                    int num = (madolst.Count >= m + maxrow) ? maxrow : madolst.Count - m;
                    lst.AddRange(madolst.GetRange(m, num));
                    sb.Append(createMokujiSqlText(s, tblname, ref id, lst));
                }
            }
            return sb.ToString();
        }

        //本文のXMLを作成する
        void createWakaXmlFiles(List<ShomeiData> shomeilst, string srcdir, string tmpdir, string xmldir)
        {
            Console.WriteLine($"srcdir={srcdir}");
            //ライブラリ番号のテキストを処理する
            /*
                収載一覧.txt
                001
                    image
                    text
                002
                    image
                    text
            */
            Encoding encsrc = Encoding.Unicode;
            foreach (ShomeiData shomei in shomeilst)
            {
                string idxdir = srcdir.combine(shomei.IndexDir); //Excelと同じレベルに 001フォルダ
                if (idxdir.existDir(false) != true)
                {
                    Log.err("wakaxml", $"ディレクトリ({idxdir})がないのでスキップします");
                    continue;
                }
                idxdir.existDir();
                //サブフォルダの全てのテキストファイルを処理する
                //まずテキストをつなげてtmpに出力
                string[] txtfiles = idxdir.getFiles("*.txt"); //その下にtextディレクトリ (モリサワ)UTF16テキストなので注意

                StringBuilder sb = new StringBuilder();
                foreach (string srcpath in txtfiles)
                {
                    sb.Append(FileUtil.getTextFromPath(srcpath, encsrc));
                }
                tmpdir.createDirIfNotExist();
                string tmppath = tmpdir.combine($"{shomei.Bango}.txt");

                FileUtil.writeTextToFile(sb.ToString(), encsrc, tmppath);
                string outpath = xmldir.combine($"{shomei.Bango}.xml"); //出力ファイル

                //XMLへ出力
                CreateWakaXmlCore core = new CreateWakaXmlCore(Log);
                core.run(tmppath, outpath);

                //XMLをチェック
                new XMLUtil(Log).checkXmlFile(outpath);
                Console.WriteLine($"==>{outpath}");
            }
        }

        void outArticles(List<ShomeiData> shomeilst, string xmldir, string outdir)
        {
            //kenkyu.webのInitDataに出力
            string outpath = outdir.combine("articles.txt");
            StringBuilder sb = new StringBuilder();
            foreach (var shomei in shomeilst)
            {
                sb.AppendLine("[data]");
                sb.AppendLine($"ID={shomei.Bango}");
                sb.AppendLine($"Title={shomei.Shomei}");
                sb.AppendLine($"Chosha={shomei.Chosha}");

                sb.AppendLine($"** Content=タイトル,レベル,ジャンプ先ID");
                List<MadoItem> madolst = createMokujiList(shomei, xmldir);
                foreach (var mado in madolst)
                {
                    sb.AppendLine($"Content={mado.text},{mado.level},{mado.href}");
                }
                sb.AppendLine("");
            }
            Console.WriteLine($"==>{outpath}");
            FileUtil.writeTextToFile(sb.ToString(), Encoding.UTF8, outpath);
        }

        string getBase64FromFile_obsolete(string binpath)
        {
            try
            {
                binpath.existFile();
                System.IO.FileStream fs =
                    new System.IO.FileStream(binpath, System.IO.FileMode.Open, System.IO.FileAccess.Read);
                byte[] bs = new byte[fs.Length];
                int readBytes = fs.Read(bs, 0, (int) fs.Length);
                fs.Close();
                return System.Convert.ToBase64String(bs); //encode
            }
            catch (Exception ex)
            {
                throw new Exception($"getBase64FromFile path={binpath} msg={ex.Message}");
            }
        }

        void outHonbunInitData(List<ShomeiData> shomeilst, string xmldir, string outdir)
        {
            //kenkyu.webのInitDataに出力
            StringBuilder sb = new StringBuilder();
            foreach (var shomei in shomeilst)
            {
                string xmlpath = xmldir.combine(shomei.FileName); //出力ファイル
                if (!xmlpath.existFile(false))
                {
                    Log.err(xmlpath, 0, "outhonbun", "xmlファイルがない");
                    continue;
                    ;
                }
                string xmlContent = FileUtil.getTextFromPath(xmlpath, Encoding.UTF8).delKaigyo(); //改行を削除

                sb.AppendLine("[data]");
                sb.AppendLine($"ID={shomei.Bango}");
                sb.AppendLine($"Fname={shomei.Bango}.xml");
                sb.AppendLine($"Content={xmlContent}");
                sb.AppendLine("");
            }
            string outpath = outdir.combine("honbun.txt");
            Console.WriteLine($"==>{outpath}");
            FileUtil.writeTextToFile(sb.ToString(), Encoding.UTF8, outpath);
        }

        public void RunText2Xml(string srcdir)
        {
            var txtdir = srcdir.combine("txt");
            var xmldir = srcdir.combine("xml").createDirIfNotExist();
            string[] srclst = txtdir.getFiles("*.txt");
            foreach (var srcpath in srclst)
            {
                Path = srcpath;
                // 05-01【竹下】《巻頭エッセイ》冷泉家時雨亭叢書の解題を執筆して.xml
                // ==> 005-01

                var outFilename = srcpath.getFileNameWithoutExtension();
                if (Regex.IsMatch(outFilename, "[0-9]{2}-[0-9]{2}"))
                {
                    outFilename = "0"+outFilename.Substring(0, 2) + "-" + outFilename.Substring(3, 2);
                }

                var outpath = xmldir.combine(outFilename + ".xml");
                Console.WriteLine($"==>{outpath}");
                //XMLへ出力
                CreateWakaXmlCore core = new CreateWakaXmlCore(Log);
                core.run(srcpath, outpath);
            }

        }

        //image
            //text
            //  001
            //  002
            public void run(string srcdir)
        {
            var txtdir = srcdir.combine("txt");
            txtdir.existDir();
            var sqldir = srcdir.combine("sql");
            sqldir.createDirIfNotExist();
            var tmpdir = srcdir.combine("tmp");
            tmpdir.createDirIfNotExist();
            var xmldir = srcdir.combine("xml");
            xmldir.existDir();
            var regdir = srcdir.combine("reg");
            regdir.createDirIfNotExist();

            //書名一覧を取得する
            Console.WriteLine("書名一覧を取得");
            List<ShomeiData> shomeilst = readShomeiList(srcdir);

            //本文のXMLを作成する SQL作る前に処理
            createWakaXmlFiles(shomeilst, txtdir, tmpdir, xmldir);

            //書名のSQLを出力
            Console.WriteLine("書名一覧をSQLに出力");
            StringBuilder sb = new StringBuilder();
            sb.Append(createShomeiSqlText(shomeilst));
            sb.AppendLine();

            //目次のSQLを出力
            Console.WriteLine("目次一覧をSQLに出力");
            sb.Append(createMokujiSqlText(shomeilst, xmldir /*作成したテキストから<窓左>を取得する*/));
            Console.WriteLine($"==>{sqldir.combine("insert.sql")}");
            FileUtil.writeTextToFile(sb.ToString(), Encoding.UTF8, sqldir.combine("insert.sql"));

            //収載一覧
            Console.WriteLine("収載一覧をSQLに出力");
            outArticles(shomeilst, xmldir, regdir);

            //XMLをDBへ登録用
            Console.WriteLine("本文をDB登録用に出力");
            outHonbunInitData(shomeilst, xmldir, regdir);
        }
    }
}
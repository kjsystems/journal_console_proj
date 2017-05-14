using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using kj.kihon;

namespace journal.console.lib.Consoles
{
  public class journal07_util : kihon_base
  {
    public journal07_util(ILogger log) : base(log)
    {
    }

    public void Run(string jobdir)
    {
      jobdir.existDir();
      var srcpath=jobdir.combine("txt").combine("iplist.txt");
      srcpath.existFile();
      Console.WriteLine($"{srcpath}");
      var outDir = jobdir.combine("out");

      //タブ区切りを読み込み
      //fukuoka	133.100.*.*
      var rd=new CSVFileReader(Log);
      rd.setupTargetToken('\t');
      var csv=rd.readFile(srcpath, Encoding.UTF8,false);
      if(csv.Rows.Count==0)
        Log.err(srcpath,0,"journal07","データがない");
      if(csv.Rows[0].Count<2)
        Log.err(srcpath, 0, "journal07", "フィールドが少ない タブ区切りで２個以上");

      //IP一覧を出力
      var outpath=outDir.combine("iplist.sql");
      Console.WriteLine($"==>{outpath}");
      FileUtil.writeTextToFile(CreateIpList(csv),Encoding.UTF8,outpath);

      //図書館一覧を出力
      outpath = outDir.combine("tosholist.sql");
      Console.WriteLine($"==>{outpath}");
      FileUtil.writeTextToFile(CreateToshoList(csv), Encoding.UTF8, outpath);
    }

    //IPアドレス一覧を登録するのSQLを作成
    string CreateIpList(CSVData csv)
    {
      var sb=new StringBuilder();
      sb.AppendLine("use journal;");
      sb.AppendLine($"insert into waka_ipadd (name,ipadd) values");
      foreach (var row in csv.Rows.Select((v,i)=>new{v,i}))
      {
        if(string.IsNullOrEmpty(row.v[0]))
          continue;
        if (row.i != 0) sb.Append(",");
        sb.Append($"('{row.v[0]}','{row.v[1]}')");
        if (row.i == csv.Rows.Count-1) sb.Append(";");
        sb.Append("\r\n");
      }
      return sb.ToString();
    }
    //IPアドレス一覧を登録するの図書館一覧を作成
    string CreateToshoList(CSVData csv)
    {
      var sb = new StringBuilder();
      sb.AppendLine("use journal;");
      foreach (var name in csv.Rows.Select(m => m[0]).Distinct())
      {
        if (string.IsNullOrEmpty(name.delKaigyo()))
          continue;
        sb.AppendLine($"insert into waka_tosholic (name,num,memo) values ('{name}',100,'{name}');");
      }
      return sb.ToString();
    }
  }
}

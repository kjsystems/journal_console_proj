using System.Text;
using kj.kihon;

namespace journal.console.lib.Consoles
{
  public class journal06_util : kihon_base
  {
    public journal06_util(ILogger log) : base(log)
    {
    }

    public void Run(string jobdir)
    {
      jobdir.existDir();
      var txtdir = jobdir.combine("txt");
      txtdir.existDir();
      var kjpdir = jobdir.combine("kjp").createDirIfNotExist();

      foreach (var srcpath in txtdir.getFiles("*.txt"))
      {
        var outpath = kjpdir.combine(srcpath.getFileNameWithoutExtension() + ".kjp");
        System.Console.WriteLine($"{srcpath}");
        System.Console.WriteLine($"==>{outpath}");

        FileUtil.writeTextToFile(CreateTextFromPath(srcpath),Encoding.UTF8, outpath);
      }
    }

    string CreateTextFromPath(string path)
    {
      var util=new TagTextUtil(Log);
      var taglst = util.parseTextFromPath(path, Encoding.UTF8);

      var sb=new StringBuilder();
      foreach (TagBase tag in taglst)
      {
        if (tag.isText())
        {
          var txt = tag.ToString()
            .Replace("", "&#x3033;")
            .Replace("α", "&#x3033;&#x3035;")
            .Replace("&#12349;", "ヽ")
            .Replace("", "&#x3035;")
            .Replace("", "&#x303b;")
            .Replace("β", "&#x303b;")
            .Replace("￥", "")
            .Replace("$", "＄")
            .Replace("＄", "&#x25e6;");
          sb.Append(txt);
          continue;
        }
        sb.Append(tag.ToString());
      }
      return sb.ToString();
    }
  }
}

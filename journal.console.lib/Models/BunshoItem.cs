using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Search;
using Microsoft.Azure.Search.Models;

namespace journal.console.lib.Models
{
  #region <文章>タグ１つをあらわす
  [SerializePropertyNamesAsCamelCase]
  public class BunshoItem
  {
    [Key]
    [IsFilterable]
    public string Id { get; set; }

    public string FileName { get; set; }

    [IsSearchable]
    public string Text { get; set; }
  }
  #endregion
}

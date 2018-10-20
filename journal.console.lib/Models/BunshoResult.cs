using System.ComponentModel.DataAnnotations;
using Microsoft.Azure.Search;
using Microsoft.Azure.Search.Models;

namespace journal.console.lib.Models
{
    /**
          検索結果
      */
    [SerializePropertyNamesAsCamelCase]
    public class BunshoResult
    {
        [Key]
        [IsFilterable]
        public string Id { get; set; }

        [IsFilterable, IsSortable]
        public string FileName { get; set; }

        [IsFilterable]
        public string Chosha { get; set; }

        //[IsSearchable, IsFilterable, IsSortable]
        public string Title { get; set; }

        //[IsSearchable, IsFilterable, IsSortable]
        public string SubTitle { get; set; }

        [IsSortable, IsFilterable]
        public int Go { get; set; }

        [IsSortable]
        public int Page { get; set; }

        //[IsSortable]
        public int JumpId { get; set; } //ジャンプ先のID

        [IsSearchable, IsFilterable, IsSortable]
        public string Text { get; set; } //表示するタグ付きテキスト
    }
}
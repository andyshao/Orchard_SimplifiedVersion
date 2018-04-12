using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NZC.WebSite.Models
{
    public class IndexModel
    {
       public IList<LunBoTu> LunBoTu { get; set; }
       public IList<TuPian> TuPian { get; set; }

    }

    public class TuPian
    {
        public string ImageUrl { get; set; }
        public string LoveCount { get; set; }
        public string PingFen { get; set; }
    }

    public class LunBoTu
    {
        public string ImageUrl { get; set; }
    }
}
using NZC.Common.Service;
using NZC.WebSite.Models;
using System.Data;
using System.Web.Http;

namespace NZC.WebSite.Controllers
{
    public class HomeController : ApiController
    {
        private readonly SqlHelper sqlHelper;
        public HomeController(SqlHelper sqlHelper)
        {
            this.sqlHelper = sqlHelper;
        }
        [HttpGet]
        public string Index()
        {
            DataTable lunbotu = sqlHelper.ExecuteDataTable("select * from NZC_LunBoTu where ShanChu='0'");
            DataTable tupian = sqlHelper.ExecuteDataTable("select * from NZC_ImageInfo order by pingfen desc");
            IndexModel Model = new IndexModel();
            Model.LunBoTu = Common.Service.ConvertHelper<LunBoTu>.DataTableConvertToList(lunbotu);
            Model.TuPian = Common.Service.ConvertHelper<TuPian>.DataTableConvertToList(tupian);
            return Newtonsoft.Json.JsonConvert.SerializeObject(Model);
        }
    }
}

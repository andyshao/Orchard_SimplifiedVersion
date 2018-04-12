using NZC.WebSite.Models;
using System.Data;
using System.Web.Http;

namespace NZC.WebSite.Controllers
{
    public class HomeController : ApiController
    {
        [HttpGet]
        public string Index()
        {
            DataTable lunbotu = Common.Service.SqlHelper.ExecuteDataTable("select * from NZC_LunBoTu where ShanChu='0'");
            DataTable tupian = Common.Service.SqlHelper.ExecuteDataTable("select * from NZC_ImageInfo");
            IndexModel Model = new IndexModel();
            Model.LunBoTu = Common.Service.ConvertHelper<LunBoTu>.DataTableConvertToList(lunbotu);
            Model.TuPian = Common.Service.ConvertHelper<TuPian>.DataTableConvertToList(tupian);
            return Newtonsoft.Json.JsonConvert.SerializeObject(Model);
        }
    }
}

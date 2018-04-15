using Aliyun.OSS;
using Newtonsoft.Json.Linq;
using NZC.Common.Service;
using Orchard.Logging;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;

namespace NZC.Member.Controllers
{
    public class ImageController : ApiController
    {
        private readonly UserFilters uf;
        private readonly OssObjectSet ossObjectSet;
        private readonly BaiDuApiHelper baiDuApiHelper;
        public ImageController(UserFilters uf, OssObjectSet OssObjectSet, BaiDuApiHelper BaiDuApiHelper)
        {
            this.uf = uf;
            this.ossObjectSet = OssObjectSet;
            this.baiDuApiHelper = BaiDuApiHelper;
        }
        [HttpPost]
        public string UpLoadFile()
        {
            if (uf.UserTF(HttpContext.Current.Request.Form["username"], HttpContext.Current.Request.Form["password"]))
            {
                string fileName = Guid.NewGuid().ToString() + ".jpg";
                try
                {
                    Stream filestream = HttpContext.Current.Request.Files["image"].InputStream;
                    ossObjectSet.PutObject(fileName, filestream);
                    return Newtonsoft.Json.JsonConvert.SerializeObject(new { Code = "30000", JsonData = fileName });
                }
                catch (Exception ex)
                {
                    return Newtonsoft.Json.JsonConvert.SerializeObject(new { Code = "30002", JsonData = ex.Message });
                }
            }
            else
            {
                return Newtonsoft.Json.JsonConvert.SerializeObject(new { Code = "30001", JsonData = "false" });
            }

        }

        [HttpPost]
        public string PingFen()
        {
            if (uf.UserTF(HttpContext.Current.Request.Form["username"], HttpContext.Current.Request.Form["password"]))
            {
                Stream filestream = HttpContext.Current.Request.Files["image"].InputStream;
                dynamic s = null;
                try
                {
                    s = baiDuApiHelper.FaceJianCe(filestream);
                }
                catch (Exception ex)
                {
                    throw ex;
                }
                string pingfen = s.result[0].beauty;
                using (NZC.Common.qds16733757_dbEntities1 db = new Common.qds16733757_dbEntities1())
                {
                    db.NZC_ImageInfo.Add(new Common.NZC_ImageInfo()
                    {
                        ImageUrl = ConfigurationManager.AppSettings["OSS.Domie"]
                                 + HttpContext.Current.Request.Form["fileName"].ToString()
                                 + ConfigurationManager.AppSettings["OSS.stylename"],
                        UserId = HttpContext.Current.Request.Form["username"],
                        LoveCount = "0",
                        PingFen = pingfen,
                        ShanChu = 0,
                    });
                    db.SaveChanges();
                }
                return Newtonsoft.Json.JsonConvert.SerializeObject(new { Code = "20000", JsonData = s });
            }
            else
            {
                return Newtonsoft.Json.JsonConvert.SerializeObject(new { Code = "20001", Message = "评分失败！" });
            }
        }
    }
}
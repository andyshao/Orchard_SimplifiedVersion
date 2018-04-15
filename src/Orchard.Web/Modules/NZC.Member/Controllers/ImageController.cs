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
        private readonly SqlHelper SQLHelper;
        public ImageController(UserFilters uf, OssObjectSet OssObjectSet, BaiDuApiHelper BaiDuApiHelper,SqlHelper sqlhelper)
        {
            this.uf = uf;
            this.ossObjectSet = OssObjectSet;
            this.baiDuApiHelper = BaiDuApiHelper;
            this.SQLHelper = sqlhelper;
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
                return Newtonsoft.Json.JsonConvert.SerializeObject(new { Code = "10002", Message = "用户名或密码错误！" });
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
                SQLHelper.ExecuteNonQuery(@"insert into NZC_ImageInfo 
                                            (ImageUrl,UserId,LoveCount,ShanChu,PingFen)
                                             values
                                            (@ImageUrl,@UserId,@LoveCount,@ShanChu,@PingFen) ",
                                            new System.Data.SqlClient.SqlParameter[] {
                                                new System.Data.SqlClient.SqlParameter("ImageUrl", 
                                                    ConfigurationManager.AppSettings["OSS.Domie"]
                                                    + HttpContext.Current.Request.Form["fileName"].ToString()
                                                    + ConfigurationManager.AppSettings["OSS.stylename"]),
                                                new System.Data.SqlClient.SqlParameter("UserId",HttpContext.Current.Request.Form["username"]),
                                                new System.Data.SqlClient.SqlParameter("LoveCount","0"),
                                                new System.Data.SqlClient.SqlParameter("PingFen",pingfen),
                                                new System.Data.SqlClient.SqlParameter("ShanChu",'0')
                                            });
                return Newtonsoft.Json.JsonConvert.SerializeObject(new { Code = "20000", JsonData = s });
            }
            else
            {
                return Newtonsoft.Json.JsonConvert.SerializeObject(new { Code = "20001", Message = "评分失败！" });
            }
        }
    }
}
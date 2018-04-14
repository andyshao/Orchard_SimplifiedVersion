using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace NZC.Common.Service
{
    public class BaiDuApiHelper:Orchard.IDependency
    {
        private readonly string APP_ID = ConfigurationManager.AppSettings["BaiDuAI.APP_ID"];
        private readonly string API_KEY = ConfigurationManager.AppSettings["BaiDuAI.API_KEY"];
        private readonly string SECRET_KEY = ConfigurationManager.AppSettings["BaiDuAI.SECRET_KEY"];
        private readonly Baidu.Aip.Face.Face Client;
        public BaiDuApiHelper()
        {
            if (this.Client == null)
            {
                Client = new Baidu.Aip.Face.Face(API_KEY, SECRET_KEY);
            }
            
            //Client.Timeout = 60000;//修改超时时间
        }

        public JObject FaceJianCe(Stream faceStream)
        {
            byte[] buffer = new byte[faceStream.Length];
            faceStream.Read(buffer, 0, (int)faceStream.Length);
            // 调用人脸检测，可能会抛出网络等异常，请使用try/catch捕获
            // 如果有可选参数
            var options = new Dictionary<string, object>{
                {"max_face_num", 1},
                {"face_fields", "beauty"}
            };
            // 带参数调用人脸检测
            try
            {
                return Client.Detect(buffer, options);
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

    }
}
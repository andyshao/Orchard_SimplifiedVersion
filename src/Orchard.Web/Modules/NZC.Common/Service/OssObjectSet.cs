using Aliyun.OSS;
using Orchard.Logging;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Web;

namespace NZC.Common.Service
{
    public  class OssObjectSet : Orchard.IDependency
    {
        private readonly string AccessKeyId= ConfigurationManager.AppSettings["OSS.AccessKeyId"];
        private readonly string AccessKeySecret= ConfigurationManager.AppSettings["OSS.AccessKeySecret"];
        private readonly string Endpoint= ConfigurationManager.AppSettings["OSS.Endpoint"];
        private readonly string BucketPrefix = ConfigurationManager.AppSettings["OSS.BucketPrefix"];
        private readonly OssClient Client;
        public OssObjectSet()
        {
            if(this.Client==null)
            {
                Client = new OssClient(Endpoint, AccessKeyId, AccessKeySecret);
            }
        }
        public  PutObjectResult PutObject(string fileName,Stream fileStream)
        {
            try
            {
                return Client.PutObject(BucketPrefix, fileName, fileStream);
            }
            catch (Exception ex)
            {

                throw ex;
            }
           
        }
    }
}
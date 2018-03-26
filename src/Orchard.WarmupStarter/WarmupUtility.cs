using System;
using System.IO;
using System.Text;
using System.Web;
using System.Web.Hosting;

namespace Orchard.WarmupStarter
{
    public static class WarmupUtility
    {
        public static readonly string WarmupFilesPath = "~/App_Data/Warmup/";
        /// <summary>
        /// 返回true以暂停请求(直到调用Signal()) -返回false以允许管道立即执行。
        /// </summary>
        /// <param name="httpApplication"></param>
        /// <returns></returns>
        public static bool DoBeginRequest(HttpApplication httpApplication)
        {
            // 使用客户端请求的url，如果它已被翻译(代理、负载均衡、…)，那么实际的url可能会有所不同。
            var url = ToUrlString(httpApplication.Request);
            var virtualFileCopy = WarmupUtility.EncodeUrl(url.Trim('/'));
            var localCopy = Path.Combine(HostingEnvironment.MapPath(WarmupFilesPath), virtualFileCopy);

            if (File.Exists(localCopy))
            {
                // 结果不应该被缓存，即使是在代理上。
                httpApplication.Response.Cache.SetExpires(DateTime.UtcNow.AddDays(-1));
                httpApplication.Response.Cache.SetValidUntilExpires(false);
                httpApplication.Response.Cache.SetRevalidation(HttpCacheRevalidation.AllCaches);
                httpApplication.Response.Cache.SetCacheability(HttpCacheability.NoCache);
                httpApplication.Response.Cache.SetNoStore();

                httpApplication.Response.WriteFile(localCopy);
                httpApplication.Response.End();
                return true;
            }

            // 没有本地副本，文件存在服务于静态文件。
            if (File.Exists(httpApplication.Request.PhysicalPath))
            {
                return true;
            }

            return false;
        }

        public static string ToUrlString(HttpRequest request)
        {
            return string.Format("{0}://{1}{2}", request.Url.Scheme, request.Headers["Host"], request.RawUrl);
        }

        public static string EncodeUrl(string url)
        {
            if (String.IsNullOrWhiteSpace(url))
            {
                throw new ArgumentException("url can't be empty");
            }

            var sb = new StringBuilder();
            foreach (var c in url.ToLowerInvariant())
            {
                // 只接受字母数字字符
                if ((c >= 'a' && c <= 'z') || (c >= '0' && c <= '9'))
                {
                    sb.Append(c);
                }
                // 否则在UTF8中编码它们。
                else
                {
                    sb.Append("_");
                    foreach (var b in Encoding.UTF8.GetBytes(new[] { c }))
                    {
                        sb.Append(b.ToString("X"));
                    }
                }
            }

            return sb.ToString();
        }
    }

}
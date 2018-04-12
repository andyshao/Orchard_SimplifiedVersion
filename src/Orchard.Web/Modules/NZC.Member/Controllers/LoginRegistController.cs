using NZC.Member.Models;
using Orchard.Logging;
using System;
using System.Web.Http;

namespace NZC.Member.Controllers
{
    public class LoginRegistController : ApiController
    {
        private readonly OrchardLog4netLogger Logger;
        public LoginRegistController(OrchardLog4netLogger logger)
        {
            Logger = logger;
        }
        [HttpPost]
        public string Regist(LoginRegist_LoginModel Model)
        {
            int usercount =Convert.ToInt32(Common.Service.SqlHelper.ExecuteSacalar("select count(*) from NZC_UserInfo where UserName=@UserName", new System.Data.SqlClient.SqlParameter("UserName", Model.UserName)));
            if(usercount==0)
            {
                try
                {
                    NZC.Common.Service.SqlHelper.ExecuteNonQuery(
               @"INSERT INTO [NZC_UserInfo]([Id],[UserName],[PassWord],[NiCheng],[JueSe])VALUES(@Id,@UserName,@PassWord,@NiCheng,@JueSe)",
               new System.Data.SqlClient.SqlParameter[] {
                    new System.Data.SqlClient.SqlParameter("Id",Guid.NewGuid()),
                    new System.Data.SqlClient.SqlParameter("UserName",Model.UserName),
                    new System.Data.SqlClient.SqlParameter("PassWord",Model.PassWord),
                    new System.Data.SqlClient.SqlParameter("NiCheng",Model.UserName),
                    new System.Data.SqlClient.SqlParameter("JueSe",1)
               });
                    return Newtonsoft.Json.JsonConvert.SerializeObject(new { Code = "10000", Message = "注册成功！" });
                }
                catch (Exception ex)
                {
                    Logger.Error(ex.Message);
                    return Newtonsoft.Json.JsonConvert.SerializeObject(new { Code = "10001", Message = "注册失败！" });
                }
            }
            else
            {
                return Newtonsoft.Json.JsonConvert.SerializeObject(new { Code="10001",Message="用户名已存在请重新输入！"});
            }
        }
        [HttpPost]
        public string UserNameRegist(string userName)
        {
            int usercount = Convert.ToInt32(Common.Service.SqlHelper.ExecuteSacalar("select count(*) from NZC_UserInfo where UserName=@UserName", new System.Data.SqlClient.SqlParameter("UserName", userName)));
            if (usercount == 0)
            {
                return Newtonsoft.Json.JsonConvert.SerializeObject(new { Code = "10000", Message = "验证通过！" });
            }
            else
            {
                return Newtonsoft.Json.JsonConvert.SerializeObject(new { Code = "10001", Message = "用户名已存在请重新输入！" });
            }
        }
        [HttpPost]
        public string Login(LoginRegist_LoginModel Model)
        {
            int usercount = Convert.ToInt32(Common.Service.SqlHelper.ExecuteSacalar("select count(*) from NZC_UserInfo where UserName=@UserName and PassWord=@PassWord", 
                new System.Data.SqlClient.SqlParameter[] {
                    new System.Data.SqlClient.SqlParameter("UserName", Model.UserName),
                    new System.Data.SqlClient.SqlParameter("PassWord",Model.PassWord) }));
            if(usercount==0)
            {
                return Newtonsoft.Json.JsonConvert.SerializeObject(new { Code = "10002", Message = "用户名或密码错误！", Result="{username:"+Model.UserName+",password:"+Model.PassWord+"}" });
            }
            else
            {
                return Newtonsoft.Json.JsonConvert.SerializeObject(new { Code = "10000", Message = "登陆成功！" });
            }
        }
    }
}

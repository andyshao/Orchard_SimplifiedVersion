using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace PTG.User.Controllers
{
    public class DefaultController : ApiController
    {
        [HttpGet]
        public string Login()
        {
            return "登陆成功";
        }
        [HttpGet]
        public string Regist()
        {
            return "注册成功";
        }
    }
}

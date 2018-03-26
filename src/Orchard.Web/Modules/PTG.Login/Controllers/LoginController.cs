using PTG.Login.ModelsDTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace PTG.Login.Controllers
{
    public class LoginController : ApiController
    {
        /// <summary>
        /// Action
        /// </summary>
        /// <param name="loginUser"></param>
        /// <returns></returns>
        // POST: api/Home
        public HttpResponseMessage Post(ModelsDTO.LoginUserInfo loginUser)
        {
            
            return PTG.Common.Services.JSON.toJson( new string[] { loginUser.password, loginUser.account });
        }


        // PUT: api/Home/5
        public void Put(int id, [FromBody]string value)
        {
        }

        // DELETE: api/Home/5
        public RetunLoginMessage Delete(int id)
        {
            return new RetunLoginMessage() { account = id.ToString() };
        }
    }
}

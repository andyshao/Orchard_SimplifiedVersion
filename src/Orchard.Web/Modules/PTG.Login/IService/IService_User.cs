using PTG.Login.ModelsDTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PTG.Login.IService
{
    public partial interface IService_Login : Orchard.IDependency
    {
        RetunLoginMessage Login(LoginUserInfo inModel);
    }
}
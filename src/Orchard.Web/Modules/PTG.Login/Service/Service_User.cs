using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using PTG.Login.ModelsDTO;

namespace PTG.Login.Service
{
    public partial class Service_User : Login.IService.IService_Login
    {
        public RetunLoginMessage Login(LoginUserInfo inModel)
        {
            RetunLoginMessage Model = new RetunLoginMessage();
            int countUser = (int)PTG.Common.Services.SqlServer2000Helper.ExecuteSacalar("select count(*) from User_Info where account=@account and password=@passsword",
                new System.Data.SqlClient.SqlParameter[] {
                    new System.Data.SqlClient.SqlParameter("account",inModel.account),
                    new System.Data.SqlClient.SqlParameter("password",inModel.password)
                });
            throw new NotImplementedException();
        }
    }
}
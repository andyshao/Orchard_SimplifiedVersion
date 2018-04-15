using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NZC.Common.Service
{
    public class UserFilters: Orchard.IDependency
    {
        private readonly SqlHelper SQLHelper;
        public UserFilters(SqlHelper sqlhelper)
        {
            SQLHelper = sqlhelper;
        }
        public bool UserTF(string username,string password)
        {
            if(!string.IsNullOrEmpty(username)&&!string.IsNullOrEmpty(password))
            {
                int t =Convert.ToInt32(SQLHelper.ExecuteSacalar("select count(*) from NZC_UserInfo where UserName=@UserName and PassWord=@PassWord",
                    new System.Data.SqlClient.SqlParameter[] {
                        new System.Data.SqlClient.SqlParameter("UserName",username),
                        new System.Data.SqlClient.SqlParameter("PassWord",password)
                    }));
                if (t == 1)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }
    }
}
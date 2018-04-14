using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NZC.Common.Service
{
    public class UserFilters:Orchard.IDependency
    {
        public bool UserTF(string username,string password)
        {
            NZC.Common.qds16733757_dbEntities1 db = new qds16733757_dbEntities1();
            if(!string.IsNullOrEmpty(username)&&!string.IsNullOrEmpty(password))
            {
                int t = db.NZC_UserInfo.Where(u => u.UserName.Equals(username) && u.PassWord.Equals(password)).Count();
                db.SaveChanges();
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
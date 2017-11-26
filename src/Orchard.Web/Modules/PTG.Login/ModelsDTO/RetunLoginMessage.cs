using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PTG.Login.ModelsDTO
{
    public partial class RetunLoginMessage
    {
        /// <summary>
        /// 状态标识 success:成功 error：失败
        /// </summary>
        public virtual string result { get; set; }
        /// <summary>
        /// 状态信息
        /// </summary>
        public virtual string cotent { get; set; }
        /// <summary>
        /// 用户名
        /// </summary>
        public virtual string account { get; set; }
    }
}
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
        public virtual string resultCode { get; set; }
        /// <summary>
        /// 状态信息
        /// </summary>
        public virtual string resultMessage { get; set; }
        /// <summary>
        /// 结果值
        /// </summary>
        public virtual string resultValue { get; set; }
    }
}
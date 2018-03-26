using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace HotplugWeb.Mvc.Filters {
    public class HotplugWebFilterProvider : System.Web.Mvc.IFilterProvider {

        public IEnumerable<Filter> GetFilters(ControllerContext controllerContext, ActionDescriptor actionDescriptor) {
            var workContext = controllerContext.GetWorkContext();

            // 将 IFilterProvider 实现映射到 MVC 筛选器对象需要提供顺序值, 
            // 因为相同作用域和顺序的筛选对象将以未定义的顺序运行。
            // 我们创建负序值以避免与其他潜在用户提供的MVC过滤对象冲突，希望使用正序值。
            // 我们通过颠倒列表和否定索引来实现这一点。
            var filters = workContext.Resolve<IEnumerable<IFilterProvider>>();
            return filters.Reverse().Select((filter, index) => new Filter(filter, FilterScope.Action, -(index + 1)));
        }
    }
}
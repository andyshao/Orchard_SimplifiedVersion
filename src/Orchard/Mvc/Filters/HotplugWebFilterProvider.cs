using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace HotplugWeb.Mvc.Filters {
    public class HotplugWebFilterProvider : System.Web.Mvc.IFilterProvider {

        public IEnumerable<Filter> GetFilters(ControllerContext controllerContext, ActionDescriptor actionDescriptor) {
            var workContext = controllerContext.GetWorkContext();

            // �� IFilterProvider ʵ��ӳ�䵽 MVC ɸѡ��������Ҫ�ṩ˳��ֵ, 
            // ��Ϊ��ͬ�������˳���ɸѡ������δ�����˳�����С�
            // ���Ǵ�������ֵ�Ա���������Ǳ���û��ṩ��MVC���˶����ͻ��ϣ��ʹ������ֵ��
            // ����ͨ���ߵ��б�ͷ�������ʵ����һ�㡣
            var filters = workContext.Resolve<IEnumerable<IFilterProvider>>();
            return filters.Reverse().Select((filter, index) => new Filter(filter, FilterScope.Action, -(index + 1)));
        }
    }
}
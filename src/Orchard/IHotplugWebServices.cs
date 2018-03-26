using HotplugWeb.Data;
using HotplugWeb.ContentManagement;
using HotplugWeb.Security;
using HotplugWeb.UI.Notify;

namespace HotplugWeb {
    /// <summary>
    /// HotplugWeb API的最重要部分
    /// </summary>
    public interface IHotplugWebServices : IDependency {
        IContentManager ContentManager { get; }
        /// <summary>
        /// 事务管理器
        /// </summary>
        ITransactionManager TransactionManager { get; }
        IAuthorizer Authorizer { get; }
        INotifier Notifier { get; }
        /// <summary>
        /// 状态工厂 Shape Factory
        /// </summary>
        /// <example>
        /// dynamic shape = New.ShapeName(Parameter: myVar)
        /// 
        /// 现在, 状态可以以各种方式使用, 
        /// 例如从 ShapeResult 中的控制器操作返回或将其添加到布局形状中。
        /// 
        /// Inside the shape template (ShapeName.cshtml) the parameters can be accessed as follows:
        /// @Model.Parameter
        /// </example>
        dynamic New { get; }

        WorkContext WorkContext { get; }
    }
}

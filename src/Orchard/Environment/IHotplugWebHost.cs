using HotplugWeb.Environment.Configuration;
using HotplugWeb.Environment.ShellBuilders;

namespace HotplugWeb.Environment {
    public interface IHotplugWebHost {
        /// <summary>
        /// 在启动时调用一次以配置应用程序域, 并加载/应用现有的 shell 配置
        /// </summary>
        void Initialize();

        /// <summary>
        /// 当明确知道已安装模块/扩展的列表已更改并且需要重新加载时, 外部调用。
        /// </summary>
        void ReloadExtensions();

        /// <summary>
        /// 每次请求开始提供实时重新初始化点时调用
        /// </summary>
        void BeginRequest();

        /// <summary>
        /// 每次请求结束时调用确定性提交并处理未完成的活动
        /// </summary>
        void EndRequest();

        ShellContext GetShellContext(ShellSettings shellSettings);

        /// <summary>
        /// 可用于生成 shell 配置代码的临时自包含实例。
        /// 可以从此实例中解决服务来配置和初始化其存储。
        /// </summary>
        IWorkContextScope CreateStandaloneEnvironment(ShellSettings shellSettings);
    }
}

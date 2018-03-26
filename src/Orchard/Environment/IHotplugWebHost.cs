using HotplugWeb.Environment.Configuration;
using HotplugWeb.Environment.ShellBuilders;

namespace HotplugWeb.Environment {
    public interface IHotplugWebHost {
        /// <summary>
        /// ������ʱ����һ��������Ӧ�ó�����, ������/Ӧ�����е� shell ����
        /// </summary>
        void Initialize();

        /// <summary>
        /// ����ȷ֪���Ѱ�װģ��/��չ���б��Ѹ��Ĳ�����Ҫ���¼���ʱ, �ⲿ���á�
        /// </summary>
        void ReloadExtensions();

        /// <summary>
        /// ÿ������ʼ�ṩʵʱ���³�ʼ����ʱ����
        /// </summary>
        void BeginRequest();

        /// <summary>
        /// ÿ���������ʱ����ȷ�����ύ������δ��ɵĻ
        /// </summary>
        void EndRequest();

        ShellContext GetShellContext(ShellSettings shellSettings);

        /// <summary>
        /// ���������� shell ���ô������ʱ�԰���ʵ����
        /// ���ԴӴ�ʵ���н�����������úͳ�ʼ����洢��
        /// </summary>
        IWorkContextScope CreateStandaloneEnvironment(ShellSettings shellSettings);
    }
}

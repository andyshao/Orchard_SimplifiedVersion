namespace HotplugWeb.Environment {
    public interface IHotplugWebShell {
        /// <summary>
        /// ����
        /// </summary>
        void Activate();
        /// <summary>
        /// ��ֹ
        /// </summary>
        void Terminate();
    }
}
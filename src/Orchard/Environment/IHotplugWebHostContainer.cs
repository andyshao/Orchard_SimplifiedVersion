namespace HotplugWeb.Environment {
    public interface IHotplugWebHostContainer {
        T Resolve<T>();
    }
}
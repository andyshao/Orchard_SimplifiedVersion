using System.Collections.Generic;
using HotplugWeb.Caching;

namespace HotplugWeb.Environment {
    /// <summary>
    /// 提供将Shims连接到HotplugWebHostContainer的能力。
    /// </summary>
    public static class HotplugWebHostContainerRegistry {
        private static readonly IList<Weak<IShim>> _shims = new List<Weak<IShim>>();
        private static IHotplugWebHostContainer _hostContainer;
        private static readonly object _syncLock = new object();

        public static void RegisterShim(IShim shim) {
            lock (_syncLock) {
                CleanupShims();

                _shims.Add(new Weak<IShim>(shim));
                shim.HostContainer = _hostContainer;
            }
        }

        public static void RegisterHostContainer(IHotplugWebHostContainer container) {
            lock (_syncLock) {
                CleanupShims();

                _hostContainer = container;
                RegisterContainerInShims();
            }
        }

        private static void RegisterContainerInShims() {
            foreach (var shim in _shims) {
                var target = shim.Target;
                if (target != null) {
                    target.HostContainer = _hostContainer;
                }
            }
        }

        private static void CleanupShims() {
            for (int i = _shims.Count - 1; i >= 0; i--) {
                if (_shims[i].Target == null)
                    _shims.RemoveAt(i);
            }
        }
    }
}

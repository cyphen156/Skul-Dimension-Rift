using System.Collections.Generic;

namespace Assets.Scripts.Managers
{
    public partial class ResourceManager
    {
        #region Type Selector
        private bool TryGetMap<T>(out Dictionary<uint, T> map) where T : class
        {
            map = null;

            object boxed;
            if (!typedMaps.TryGetValue(typeof(T), out boxed))
            {
                return false;
            }

            map = boxed as Dictionary<uint, T>;
            if (map == null)
            {
                Debug.Assert(false, $"[ResourceManager] typedMaps has wrong map type for {typeof(T).Name}.");
                return false;
            }

            return true;
        }
    }
    #endregion
}
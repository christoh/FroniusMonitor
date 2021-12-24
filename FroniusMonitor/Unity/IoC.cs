using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity;

namespace De.Hochstaetter.FroniusMonitor.Unity
{
    public static class IoC
    {
        public static T TryGet<T>()
        {
            try
            {
                return App.Container.Resolve<T>();
            }
            catch (Exception ex)
            {
                return default(T)!;
            }
        }
        public static T Get<T>()
        {
            return App.Container.Resolve<T>();
        }
    }
}

using System;
using System.Threading.Tasks;

namespace Common.Extensions
{
    public static class TaskExtenstions
    {
        public static async Task<V> Bind<U, V>(this Task<U> m, Func<U, Task<V>> k)
        {
            return await k(await m);
        }
    }
}

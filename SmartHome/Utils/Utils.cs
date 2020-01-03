using System;
using System.Collections.Generic;

namespace SmartHome.Utils
{
    public static class Utils
    {
        public static void Foreach<T>(this IEnumerable<T> items, Action<T> action)
        {
            foreach (var item in items) action(item);
        }
    }
}
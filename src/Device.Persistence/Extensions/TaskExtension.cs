using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Device.Persistence.Extensions
{
    public static class TaskExtension
    {
        public static Task<IEnumerable<T>> HandleResult<T>(this Task<IEnumerable<T>> task)
        {
            return task.ContinueWith((t) =>
            {
                if (t.IsCanceled || t.IsFaulted)
                    return Task.FromResult((IEnumerable<T>)Array.Empty<T>());
                else
                    return t;
            }).Unwrap();
        }

        public static Task<(IEnumerable<T> Series, int TotalCount)> HandleResult<T>(this Task<(IEnumerable<T> Series, int TotalCount)> task)
        {
            return task.ContinueWith((t) =>
            {
                if (t.IsCanceled || t.IsFaulted)
                    return Task.FromResult(((IEnumerable<T>)Array.Empty<T>(), 0));
                else
                    return t;
            }).Unwrap();
        }
    }
}

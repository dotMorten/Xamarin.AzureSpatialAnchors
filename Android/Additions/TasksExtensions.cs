
using Android.Runtime;
using Java.Util.Concurrent;

namespace Microsoft.Azure.SpatialAnchors.Extensions
{
    internal static class TasksExtensions
    {
        private static readonly IExecutorService executorService = Executors.NewFixedThreadPool(2); 

        public static async System.Threading.Tasks.Task<TResult> AsAsync<TResult>(this IFuture task) where TResult : Java.Lang.Object
        {
            var result = await task.GetAsync().ConfigureAwait(false);
            return (TResult)result;
        }
        public static async System.Threading.Tasks.Task<string> AsAsyncString(this IFuture task) 
        {
            var result = await task.GetAsync().ConfigureAwait(false);
            return (string)result;
        }

        public static System.Threading.Tasks.Task AsAsync(this IFuture task)
        {
            return task.GetAsync();
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace Microsoft.Azure.SpatialAnchors
{
    public partial class CloudSpatialAnchorSession
    {
        
        //public virtual Task CreateAnchorAsync2 (global::Microsoft.Azure.SpatialAnchors.CloudSpatialAnchor anchor)
        //{
        //    var future = CreateAnchorAsync(anchor);
        //    var tcs = new TaskCompletionSource<object>();
        //    Java.Util.Concurrent.CompletableFuture.SupplyAsync((s) =>
        //    {
        //        try
        //        {
        //            future.Get();
        //            tcs.SetCompleted(null);
        //        }
        //        catch (Java.Lang.InterruptedException ex)
        //        {
        //            tcs.SetCanceled();
        //        }
        //        catch(Exception ex)
        //        {
        //            tcs.SetException(ex);
        //        }
        //    });
        //    return tcs.Task;
        //}

    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace TestApp
{
    class AnchorPoster
    {
        private readonly string baseAddress;
        public AnchorPoster(string baseAddress)
        {
            this.baseAddress = baseAddress;
        }

        public async Task<string> PostAnchorAsync(string anchor)
        {
            try
            {
                Uri url = new Uri(baseAddress);
                using (HttpClient client = new HttpClient())
                {
                    var response = await client.PostAsync(baseAddress, new StringContent(anchor)).ConfigureAwait(false);
                    return await response.EnsureSuccessStatusCode().Content.ReadAsStringAsync().ConfigureAwait(false);
                }
            }
            catch (Exception e)
            {
                return e.Message;
            }
        }
    }
}
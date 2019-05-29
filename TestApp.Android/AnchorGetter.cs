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
    class AnchorGetter 
    {
        private readonly string baseAddress;

        public AnchorGetter(string baseAddress)
        {
            this.baseAddress = baseAddress;
        }

        public async Task<string> GetAnchor(string AnchorNumber)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    return await client.GetStringAsync(baseAddress + "/" + AnchorNumber).ConfigureAwait(false);
                }
            }
            catch (Exception e)
            {
                return e.Message;
            }
        }
    }
}
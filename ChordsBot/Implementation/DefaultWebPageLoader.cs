using System;
using System.Net.Http;
using System.Threading.Tasks;
using ChordsBot.Common;
using ChordsBot.Interfaces;

namespace ChordsBot.Implementation
{
    public class DefaultWebPageLoader : IWebPageLoader
    {
        public async Task<Result<string>> Load(Uri url)
        {
            using (var httpClient = new HttpClient())
            {
                try
                {
                    var result = await httpClient.GetStringAsync(url);

                    return result.Return();
                }
                catch (Exception ex)
                {
                    return Result<string>.Error(ex.Message);
                }
            }
        }
    }
}

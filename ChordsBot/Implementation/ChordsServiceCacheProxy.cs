using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ChordsBot.Common;
using ChordsBot.Interfaces;
using ChordsBot.Models;

namespace ChordsBot.Implementation
{
    public class ChordsServiceCacheProxy : IChordsService
    {
        private static readonly IDictionary<string, ChordsSearchResults> Cache;
        private readonly IChordsService _decoratee;

        static ChordsServiceCacheProxy()
        {
            Cache = new ConcurrentDictionary<string, ChordsSearchResults>();
        }

        public ChordsServiceCacheProxy(IChordsService decoratee)
        {
            _decoratee = decoratee;
        }

        public async Task<ChordsSearchResults> FindChords(string query)
        {
            var key = GetCacheKey(query);

            return !Cache.ContainsKey(key)
                ? Cache[key] = await _decoratee.FindChords(query)
                : Cache[key];
        }

        public async Task<IResult<ChordsLink>> FindFirst(string query)
        {
            var key = GetCacheKey(query);

            return Cache.ContainsKey(key)
                ? Cache[key].Results.FirstOrDefault().Return()
                : await _decoratee.FindFirst(query);
        }

        public Task<IResult<Chords>> Get(ChordsLink chordsLInk)
        {
            return _decoratee.Get(chordsLInk);
        }

        private static string GetCacheKey(string query) => query.Replace(' ', '_');
    }
}
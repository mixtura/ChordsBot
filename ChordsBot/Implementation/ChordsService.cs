using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ChordsBot.Common;
using ChordsBot.Interfaces;
using ChordsBot.Models;

namespace ChordsBot.Implementation
{
    public class ChordsService : IChordsService
    {
        private readonly IReadOnlyCollection<IChordsGrabber> _chordsGrabbers; 

        public ChordsService(IReadOnlyCollection<IChordsGrabber> chordsGrabbers) 
        {
            _chordsGrabbers = chordsGrabbers;
        }

        public async Task<ChordsSearchResults> FindChords(string query)
        {
            var results = await Task.WhenAll(_chordsGrabbers.Select(x => x.GrabLinks(query)).ToArray());
            var finalResult = new List<ChordsLink>();

            foreach (var result in results)
            {
                result.MatchResult(x => finalResult.AddRange(x));
            }

            return new ChordsSearchResults(query, finalResult, DateTime.UtcNow);
        }

        public async Task<IResult<ChordsLink>> FindFirst(string query)
        { 
            var error = Result<ChordsLink>.Error("chords not found");
            var tasks = _chordsGrabbers.Select(x => x.GrabLinks(query)).ToList();

            if (!tasks.Any())
            {
                return error;
            }

            var result = await GetFirst(tasks.First(), tasks.Skip(1).ToList());

            return result;
        }

        public async Task<IResult<Chords>> Get(ChordsLink chordsLInk)
        {
            var error = Task.FromResult(Result<Chords>.Error("invalid link"));

            return await _chordsGrabbers
                .Where(x => x.CanGrab(chordsLInk.Origin))
                .Take(1)
                .Select(async x => (await x.GrabChords(chordsLInk.Url))
                    .Bind(y => new Chords(y, chordsLInk)
                    .Return())
                )
                .Aggregate(error, (x, y) => y);
        }

        private static async Task<IResult<ChordsLink>> GetFirst(Task<IResult<List<ChordsLink>>> task,
            IList<Task<IResult<List<ChordsLink>>>> rest)
        {
            var result = await task;
            var singleResult = result.Bind(x => x.FirstOrDefault().Return());

            if (!rest.Any())
            {
                return singleResult;
            }

            return singleResult.ContainsValue
                ? singleResult
                : await GetFirst(rest.First(), rest.Skip(1).ToList());
        }
    }
}
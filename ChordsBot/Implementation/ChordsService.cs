using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Parser.Common;
using Parser.Interfaces;
using Parser.Models;

namespace Parser.Implementation
{
    public class ChordsService : IChordsService
    {
        private IReadOnlyCollection<IChordsGrabber> _chordsGrabbers; 

        public ChordsService(IReadOnlyCollection<IChordsGrabber> chordsGrabbers) 
        {
            _chordsGrabbers = chordsGrabbers;
        }

        public async Task<ChordsSearchResults> FindChords(string query)
        {
            var finalResult = new List<ChordsLink>();

            // can I get rid of side effect? should I?
            foreach(var grabber in _chordsGrabbers) 
            {
                var links = await grabber.GrabLinks(query);

                links.MatchResult(x => finalResult.AddRange(x));
            }

            return new ChordsSearchResults(finalResult, DateTime.UtcNow);
        }

        public async Task<Result<ChordsLink>> FindFirst(string query)
        {            
            var finalResult = new List<ChordsLink>();
            var error = Result<ChordsLink>.Error("chords not found");

            // can I get rid of side effect? should I?
            foreach(var grabber in _chordsGrabbers) 
            {
                var links = await grabber.GrabLinks(query);

                // critical. could be error otherwise
                links.MatchResult(x => finalResult.AddRange(x.Take(1)));

                if(finalResult.Any())
                {
                    break;
                }
            }
            
            return finalResult.Aggregate(error, (x, y) => x = y.Return());
        }

        public async Task<Result<string>> Get(ChordsLink chordsLInk)
        {
            var error = Task.FromResult(Result<string>.Error("invalid link"));

            return await _chordsGrabbers
                .Where(x => x.CanGrab(chordsLInk.Origin))
                .Take(1)
                .Select(async x => await x.GrabChords(chordsLInk.Url))
                .Aggregate(error, (x, y) => x = y);
        }
    }
}
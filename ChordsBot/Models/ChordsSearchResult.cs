using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Parser.Common;

namespace Parser.Models
{
    public class ChordsSearchResults
    {
        public ChordsSearchResults(List<ChordsLink> results, DateTime date) 
        {
            Results = results ?? throw new ArgumentNullException(nameof(results));;
            Date = date;
        }

        public DateTime Date { get; }
        public List<ChordsLink> Results { get; }
    }
}

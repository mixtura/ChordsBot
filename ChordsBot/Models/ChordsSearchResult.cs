using System;
using System.Collections.Generic;

namespace ChordsBot.Models
{
    public class ChordsSearchResults
    {
        public ChordsSearchResults(string query, List<ChordsLink> results, DateTime date)
        {
            Query = query ?? throw new ArgumentNullException(nameof(query));
            Results = results ?? throw new ArgumentNullException(nameof(results));
            Date = date;
        }
        
        public string Query { get; }
        public DateTime Date { get; }
        public List<ChordsLink> Results { get; }
    }
}

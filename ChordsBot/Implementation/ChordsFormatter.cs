using System;
using System.Net.Http;
using System.Threading.Tasks;
using Parser.Common;
using Parser.Interfaces;
using Parser.Models;

namespace Parser.Implementation
{
    public class ChordsFormatter : IChordsFormatter
    {
        public string Format(ChordsLink link, string chords)
        {
            var header = GetHeader(link);
            var body = GetBody(chords);

            return $"{header} \n {body}";
        }

        private string GetHeader(ChordsLink link)
        {            
            return $"{link.SongAuthor} - {link.SongName} \n" 
                + $"Origin: {link.Origin} \n"
                + $"Url: {link.Url} \n";            
        }

        private string GetBody(string chords) 
        {
            return chords;
        }
    }
}
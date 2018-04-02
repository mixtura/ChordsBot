using ChordsBot.Interfaces;
using ChordsBot.Models;

namespace ChordsBot.Implementation
{
    public class ChordsFormatter : IChordsFormatter
    {
        public string Format(ChordsLink link, string chords)
        {
            var header = GetHeader(link);
            var body = GetBody(chords);

            return $"{header} \n {body}";
        }

        private static string GetHeader(ChordsLink link)
        {            
            return $"{link.SongAuthor} - {link.SongName} \n" 
                + $"Origin: {link.Origin} \n"
                + $"Url: {link.Url} \n";            
        }

        private static string GetBody(string chords) 
        {
            return chords;
        }
    }
}
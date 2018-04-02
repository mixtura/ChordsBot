using System;

namespace ChordsBot.Models
{
    public class ChordsLink
    {
        public ChordsLink(Uri origin, Uri url, string songName, string songAuthor) 
        {
            Origin = origin ?? throw new ArgumentNullException(nameof(origin));
            Url = url ?? throw new ArgumentNullException(nameof(url)); 
            SongName = songName ?? throw new ArgumentNullException(nameof(songName));
            SongAuthor = songAuthor ?? throw new ArgumentNullException(nameof(songAuthor));
        }

        public string Id => Url.ToString();
        public Uri Origin { get; }
        public Uri Url { get; }
        public string SongName { get; }
        public string SongAuthor { get; }
    }
}
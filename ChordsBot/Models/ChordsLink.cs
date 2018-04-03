using System;

namespace ChordsBot.Models
{
    public class ChordsLink
    {
        public ChordsLink(Uri origin, Uri thumbnail, Uri url, string songName, string songAuthor)
        {
            Thumbnail = thumbnail ?? throw new ArgumentNullException(nameof(thumbnail));
            Origin = origin ?? throw new ArgumentNullException(nameof(origin));
            Url = url ?? throw new ArgumentNullException(nameof(url)); 
            SongName = GuardStringValue(songName, nameof(songName));
            SongAuthor = GuardStringValue(songAuthor, nameof(songAuthor));
        }

        public string Id => Url.ToString();
        public Uri Thumbnail { get; set; }
        public Uri Origin { get; }
        public Uri Url { get; }
        public string SongName { get; }
        public string SongAuthor { get; }

        private static string GuardStringValue(string value, string name)
        {
            return string.IsNullOrWhiteSpace(value)
                ? throw new ArgumentException($"{name} can't be null or white space")
                : value;
        }
    }
}
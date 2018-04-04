namespace ChordsBot.Models
{
    public class Chords
    {
        // TODO: To implement
        private const string ChordsPattern = @"([CDEFGAB])([67])?([Mmb#]+)?([67])?([Mmb#]+)?\b";

        public Chords(string chords, ChordsLink sourceLink)
        {
            SourceLink = sourceLink;
            RawChords = chords;
        }

        public string RawChords { get; }
        public ChordsLink SourceLink { get; }
    }
}
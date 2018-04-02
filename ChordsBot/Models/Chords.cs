using System.Linq;
using System.Text.RegularExpressions;

namespace ChordsBot.Models
{
    // TODO: Implement

    public enum Note { C = 0, D, E, F, G, A, B }
    public enum Suffix { None, Sharp, Flat }

    public class Chord
    {
        private readonly Note _note;
        private readonly Suffix _suffix;
        private readonly Chord _next;
        private readonly Chord _prev;

        public Chord(Note note, Suffix suffix)
        {
            _note = note;
            _suffix = suffix;
        }
    }

    public class Chords
    {
        // TODO: Add H Chords
        private const string Keys = "CDEFGAB";
        private const string ChordsPattern = @"([CDEFGAB])([67])?([Mmb#]+)?([67])?([Mmb#]+)?\b";

        public Chords(string chords)
        {
            RawChords = chords;
        }

        public string RawChords { get; }

        public string GetLyrics()
        {
            return string.Empty;
        }

        public string ToString(int keyShift = 0)
        {
            return string.Empty;

        }

        private string ShiftKey(string key, int shift)
        {
            return string.Empty;
        }
    }
}

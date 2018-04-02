using ChordsBot.Models;

namespace ChordsBot.Interfaces
{
    public interface IChordsFormatter
    {
        string Format(ChordsLink link, string chords);
    }
}
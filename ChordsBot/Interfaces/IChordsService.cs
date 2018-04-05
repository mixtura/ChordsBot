using System.Threading.Tasks;
using ChordsBot.Common;
using ChordsBot.Models;

namespace ChordsBot.Interfaces
{
    public interface IChordsService
    {
        Task<ChordsSearchResults> FindChords(string query);
        Task<IResult<ChordsLink>> FindFirst(string query);
        Task<IResult<Chords>> Get(ChordsLink chordsLInk);
    }
}
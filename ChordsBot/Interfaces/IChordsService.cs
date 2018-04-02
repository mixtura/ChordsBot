using System.Threading.Tasks;
using ChordsBot.Common;
using ChordsBot.Models;

namespace ChordsBot.Interfaces
{
    public interface IChordsService
    {
        Task<ChordsSearchResults> FindChords(string query);
        Task<Result<ChordsLink>> FindFirst(string query);
        Task<Result<string>> Get(ChordsLink chordsLInk);
    }
}
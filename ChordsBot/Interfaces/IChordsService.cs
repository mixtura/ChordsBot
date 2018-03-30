using System.Threading.Tasks;
using Parser.Common;
using Parser.Models;

namespace Parser.Interfaces
{
    public interface IChordsService
    {
        Task<ChordsSearchResults> FindChords(string query);
        Task<Result<ChordsLink>> FindFirst(string query);
        Task<Result<string>> Get(ChordsLink chordsLInk);
    }
}
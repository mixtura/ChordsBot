using System.Threading.Tasks;
using Parser.Common;

namespace Parser.Interfaces
{
    public interface IChordsFinder
    {
        Task<Result<string>> FindChords(string query);
    }
}

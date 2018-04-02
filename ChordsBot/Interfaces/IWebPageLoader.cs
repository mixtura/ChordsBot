using System;
using System.Threading.Tasks;
using ChordsBot.Common;

namespace ChordsBot.Interfaces
{
    public interface IWebPageLoader
    {
        Task<Result<string>> Load(Uri url);
    }
}
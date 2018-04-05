using System;
using System.Threading.Tasks;
using ChordsBot.Common;

namespace ChordsBot.Interfaces
{
    public interface IWebPageLoader
    {
        Task<IResult<string>> Load(Uri url);
    }
}
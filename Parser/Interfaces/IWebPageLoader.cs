using System;
using System.Threading.Tasks;
using Parser.Common;

namespace Parser.Interfaces
{
    public interface IWebPageLoader
    {
        Task<Result<string>> Load(Uri url);
    }
}
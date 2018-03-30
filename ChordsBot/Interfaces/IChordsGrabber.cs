using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Parser.Common;
using Parser.Models;

namespace Parser.Interfaces
{
    public interface IChordsGrabber
    {
        Task<Result<List<ChordsLink>>> GrabLinks(string query);
        Task<Result<string>> GrabChords(Uri url);
        bool CanGrab(Uri origin);
    }
}
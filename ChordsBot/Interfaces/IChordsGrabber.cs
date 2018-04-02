using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ChordsBot.Common;
using ChordsBot.Models;

namespace ChordsBot.Interfaces
{
    public interface IChordsGrabber
    {
        Task<Result<List<ChordsLink>>> GrabLinks(string query);
        Task<Result<string>> GrabChords(Uri url);
        bool CanGrab(Uri origin);
    }
}
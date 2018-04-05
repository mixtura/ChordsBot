using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ChordsBot.Common;
using ChordsBot.Models;

namespace ChordsBot.Interfaces
{
    public interface IChordsGrabber
    {
        Task<IResult<List<ChordsLink>>> GrabLinks(string query);
        Task<IResult<string>> GrabChords(Uri url);
        bool CanGrab(Uri origin);
    }
}
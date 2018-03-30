using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Parser.Common;
using Parser.Models;

namespace Parser.Interfaces
{
    public interface IChordsFormatter
    {
        string Format(ChordsLink link, string chords);
    }
}
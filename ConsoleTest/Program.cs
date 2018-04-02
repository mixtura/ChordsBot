using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ChordsBot.Implementation;
using ChordsBot.Interfaces;

namespace ConsoleTest
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            while (true)
            {
                var input = Console.ReadLine();

                if (string.IsNullOrEmpty(input))
                {
                    break;
                }

                FindAndPrintChords(input).Wait();
            }
        }

        private static async Task FindAndPrintChords(string query)
        {
            var service = GetService();
            var result = await service.FindFirst(query);
            var print = (Action<string>) Console.WriteLine;

            await result.MatchResult(async x => 
            {
                var chords = await service.Get(x);                
                
                chords.Match(print, print);
            });
        }

        private static IChordsService GetService() 
        {            
            var loader = new DefaultWebPageLoader();
            var grabbers = new List<IChordsGrabber> { new EChordsGrabber(loader) };
            var service = new ChordsService(grabbers);

            return service;
        }
    }
}
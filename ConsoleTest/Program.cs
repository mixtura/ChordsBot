using System;
using System.Threading.Tasks;
using Parser.Implementation.EChords;

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
            var finder = new EChordsFinder();
            var result = await finder.FindChords(query);
            var print = (Action<string>) Console.WriteLine;

            result.Match(print, print);
        }
    }
}
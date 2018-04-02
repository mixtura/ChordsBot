using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ChordsBot.Common;
using ChordsBot.Interfaces;
using ChordsBot.Models;
using HtmlAgilityPack;

namespace ChordsBot.Implementation
{
    public class MyChordsGrabber : IChordsGrabber
    {        
        private readonly Uri _mychordsUrl = new Uri("https://mychords.net");
        private readonly IWebPageLoader _webPageLoader;

        public MyChordsGrabber(IWebPageLoader webPageLoader) 
        {
            _webPageLoader = webPageLoader ?? throw new ArgumentNullException(nameof(webPageLoader));
        }

        public async Task<Result<List<ChordsLink>>> GrabLinks(string query) 
        {            
            var searchUrl = new Uri(_mychordsUrl, $"/search?q={query}");
            var page = await _webPageLoader.Load(searchUrl);

            return page.Bind(ToSafe(ExtractLinks));
        }

        public async Task<Result<string>> GrabChords(Uri url) 
        {
            var page = await _webPageLoader.Load(url);

            return page.Bind(ToSafe(ExtractChords));
        }

        public bool CanGrab(Uri origin) => _mychordsUrl == origin;

        private List<ChordsLink> ExtractLinks(string content)
        {            
            var document = new HtmlDocument();

            document.LoadHtml(content);

            var links = document.DocumentNode
                .Descendants("a")
                .Where(x => x.HasClass("b-listing__item__link"))
                .Select(x => {
                    var url = new Uri(_mychordsUrl, x.GetAttributeValue("href", ""));
                    var names =  x.InnerText
                        .Split('–', '-')
                        .Select(n => n.Trim('\n', '\t', ' '))
                        .ToArray();

                    return new ChordsLink(_mychordsUrl, url, names[1], names[0]);
                });
                
            return links.ToList();
        }
        private static string ExtractChords(string content)
        {
            var document = new HtmlDocument();

            document.LoadHtml(content);

            return document.DocumentNode
                .Descendants("pre")
                .Single(x => x.HasClass("w-words__text"))
                .InnerText;
        }

        private static Func<string, Result<T>> ToSafe<T>(Func<string, T> func)
        {
            return content =>
            {
                try
                {
                    return func(content).Return();
                }
                catch (Exception ex)
                {
                    return Result<T>.Error(ex.Message);
                }
            };
        }
    }
}

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
        private readonly Uri _thumbnail = new Uri("https://mychords.net/i/img/head/logo_image_src.png");
        private readonly IWebPageLoader _webPageLoader;

        public MyChordsGrabber(IWebPageLoader webPageLoader) 
        {
            _webPageLoader = webPageLoader ?? throw new ArgumentNullException(nameof(webPageLoader));
        }

        public async Task<IResult<List<ChordsLink>>> GrabLinks(string query) 
        {            
            var searchUrl = new Uri(_mychordsUrl, $"/search?q={query}");
            var page = await _webPageLoader.Load(searchUrl);

            return page.Bind(ToSafe(ExtractLinks));
        }

        public async Task<IResult<string>> GrabChords(Uri url) 
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
                    // TODO: need to wrap this in Result, so that if error happens then it won't fail whole search
                    var url = new Uri(_mychordsUrl, x.GetAttributeValue("href", ""));
                    var names = x.InnerText
                        .Split('–', '-')
                        .Select(n => n.Trim('\n', '\t', ' '))
                        .ToArray();

                    var songName = names.Length > 1 ? names[1] : names[0];
                    var authorName = names.Length > 1 ? names[0] : "Unknown";

                    return new ChordsLink(_mychordsUrl, _thumbnail, url, songName, authorName);
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

        private static Func<string, IResult<T>> ToSafe<T>(Func<string, T> func)
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

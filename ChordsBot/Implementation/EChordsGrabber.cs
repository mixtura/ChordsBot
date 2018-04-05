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
    public class EChordsGrabber : IChordsGrabber
    {        
        private readonly Uri _echordsUrl = new Uri("https://www.e-chords.com");
        private readonly Uri _thumbnail = new Uri("https://pbs.twimg.com/profile_images/3003366372/5505c019c3712bf71678151405f96e51_400x400.jpeg");
        private readonly IWebPageLoader _webPageLoader;

        public EChordsGrabber(IWebPageLoader webPageLoader) 
        {
            _webPageLoader = webPageLoader ?? throw new ArgumentNullException(nameof(webPageLoader));
        }

        public async Task<IResult<List<ChordsLink>>> GrabLinks(string query) 
        {            
            var searchUrl = new Uri(_echordsUrl, $"/search-all/{query}");
            var page = await _webPageLoader.Load(searchUrl);

            return page.Bind(ToSafe(ExtractLinks));
        }

        public async Task<IResult<string>> GrabChords(Uri url) 
        {
            var page = await _webPageLoader.Load(url);

            return page.Bind(ToSafe(ExtractChords));
        }

        public bool CanGrab(Uri origin) => _echordsUrl == origin;

        private List<ChordsLink> ExtractLinks(string content)
        {            
            var document = new HtmlDocument();

            document.LoadHtml(content);

            var linkNodes = document.GetElementbyId("results").ChildNodes
                .Where(x => x.Name == "li")
                .SelectMany(x => x.ChildNodes)
                .Where(x => x.HasClass("lista"))
                .ToList();

            var links = new List<ChordsLink>();
            
            foreach (var linkNode in linkNodes)
            {
                if (TryGetChordsLink(linkNode, out ChordsLink chordsLink))
                {
                    links.Add(chordsLink);
                }
            }

            return links;
        }

        private bool TryGetChordsLink(HtmlNode node, out ChordsLink chordsLink) 
        {
            var children = node.ChildNodes;
            var linkNode = children.Single(child => child.HasClass("types")).ChildNodes
                .SingleOrDefault(x => x.HasClass("ta"));
            var nameNode = children.Single(child => child.HasClass("h1")).ChildNodes
                .Single(child => child.Name == "a");
            var authorNode = children.Single(child => child.HasClass("h2")).ChildNodes
                .Single(child => child.Name == "a");
            
            var url = linkNode?.GetAttributeValue("href", string.Empty);
            var name = nameNode.InnerText;
            var author = authorNode.InnerText;

            if (string.IsNullOrWhiteSpace(url))
            {
                chordsLink = null;
                return false;
            }

            chordsLink = new ChordsLink(_echordsUrl, _thumbnail, new Uri(url), name, author);
            return true;
        }

        private static string ExtractChords(string content)
        {
            var document = new HtmlDocument();

            document.LoadHtml(content);

            return document.GetElementbyId("core")?.InnerText;
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

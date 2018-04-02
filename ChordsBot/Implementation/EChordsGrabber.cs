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
        private readonly IWebPageLoader _webPageLoader;

        public EChordsGrabber(IWebPageLoader webPageLoader) 
        {
            _webPageLoader = webPageLoader ?? throw new ArgumentNullException(nameof(webPageLoader));
        }

        public async Task<Result<List<ChordsLink>>> GrabLinks(string query) 
        {            
            var searchUrl = new Uri(_echordsUrl, $"/search-all/{query}");
            var page = await _webPageLoader.Load(searchUrl);

            return page.Bind(ToSafe(ExtractLinks));            
        }

        public async Task<Result<string>> GrabChords(Uri url) 
        {
            var page = await _webPageLoader.Load(url);

            return page.Bind(ToSafe(ExtractChords));
        }

        public bool CanGrab(Uri origin) => _echordsUrl == origin;

        private Result<List<ChordsLink>> ExtractLinks(string content)
        {            
            var document = new HtmlDocument();

            document.LoadHtml(content);

            var links = document.GetElementbyId("results").ChildNodes
                .Where(x => x.Name == "li")
                .SelectMany(x => x.ChildNodes)
                .Where(x => x.HasClass("lista"))
                .Select(NodeToChordsLink)
                .ToList()
                .Return();

            return links;
        }

        private ChordsLink NodeToChordsLink(HtmlNode node) 
        {
            var children = node.ChildNodes;
            var nameNode = children.Single(child => child.HasClass("h1")).ChildNodes
                .Single(child => child.Name == "a");
            var authorNode = children.Single(child => child.HasClass("h2")).ChildNodes
                .Single(child => child.Name == "a");

            var url = nameNode.GetAttributeValue("href", "");
            var name = nameNode.InnerText;
            var author = authorNode.InnerText;
            
            return new ChordsLink(_echordsUrl, new Uri(url), name, author);
        }

        private Result<string> ExtractChords(string content)
        {
            var document = new HtmlDocument();

            document.LoadHtml(content);

            return document
                .GetElementbyId("core")?.InnerText
                .Return();
        }

        private static Func<string, Result<T>> ToSafe<T>(Func<string, Result<T>> func)
        {
            return content =>
            {
                try
                {
                    return func(content);
                }
                catch (Exception ex)
                {
                    return Result<T>.Error(ex.Message);
                }
            };
        }
    }
}

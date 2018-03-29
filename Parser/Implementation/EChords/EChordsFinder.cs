using HtmlAgilityPack;
using Parser.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Parser.Common;

namespace Parser.Implementation.EChords
{
    public class EChordsFinder : IChordsFinder
    {
        private readonly Uri _echordsUrl = new Uri("https://www.e-chords.com");

        public async Task<Result<string>> FindChords(string query)
        {
            var webPageLoader = new DefaultWebPageLoader();
            var searchUrl = new Uri(_echordsUrl, $"/search-all/{query}");

            return await new WebPageCrawler(webPageLoader)
                .Navigate(searchUrl, ToSafe(ExtractSearchResult))
                .Navigate(x => x.FirstOrDefault(), ToSafe(ExtractChords))
                .GetResult();
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

        private static Result<List<Uri>> ExtractSearchResult(string content)
        {
            const string resultsId = "results";
            var document = new HtmlDocument();

            document.LoadHtml(content);

            var links = document.GetElementbyId(resultsId).ChildNodes
                .Where(x => x.Name == "li")
                .SelectMany(x => x.ChildNodes)
                .Where(x => x.HasClass("lista"))
                .Select(x => x.ChildNodes
                    .Single(child => child.HasClass("h1")).ChildNodes
                    .Single(child => child.Name == "a")
                    .GetAttributeValue("href", ""))
                .Select(x => new Uri(x)).ToList();

            return links.Any()
                ? links.Return()
                : Result<List<Uri>>.Error("Search failed.");
        }

        private static Result<string> ExtractChords(string content)
        {
            const string contentId = "core";
            var document = new HtmlDocument();

            document.LoadHtml(content);

            return document.GetElementbyId(contentId)
                ?.InnerText
                .Return();
        }
    }
}

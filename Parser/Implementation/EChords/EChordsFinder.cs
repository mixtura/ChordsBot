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
        private class EChordsSearchResults : IContentExtractStrategy<List<Uri>>
        {
            private const string ResultsId = "results";

            public Result<List<Uri>> ExtractData(string content)
            {
                try
                {
                    return ExctractDataInternal(content);
                }
                catch (Exception ex)
                {
                    return Result<List<Uri>>.Error(ex.Message);
                }
            }

            private Result<List<Uri>> ExctractDataInternal(string content)
            {
                var document = new HtmlDocument();

                document.LoadHtml(content);

                var links = document.GetElementbyId(ResultsId).ChildNodes
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
        }

        private class EChordsPage : IContentExtractStrategy<string>
        {
            private const string ContentId = "core";

            public Result<string> ExtractData(string content)
            {
                var document = new HtmlDocument();

                document.LoadHtml(content);

                return document.GetElementbyId(ContentId)
                    ?.InnerText
                    .Return();
            }
        }

        private readonly Uri _echordsUrl = new Uri("https://www.e-chords.com");

        public async Task<Result<string>> FindChords(string query)
        {
            var webPageLoader = new DefaultWebPageLoader();

            return await new WebPageCrawler(webPageLoader)
                .Navigate(new Uri(_echordsUrl, $"/search-all/{query}"), new EChordsSearchResults())
                .Navigate(x => x.FirstOrDefault(), new EChordsPage())
                .GetResult();
        }
    }
}

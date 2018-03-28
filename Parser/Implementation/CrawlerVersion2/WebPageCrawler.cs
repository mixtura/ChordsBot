using System;
using System.Threading.Tasks;
using Parser.Common;
using Parser.Interfaces;

namespace Parser.Implementation.CrawlerVersion2
{
    public class WebPageCrawler : IWebPageCrawler
    {
        private readonly IWebPageLoader _webPageLoader;

        public WebPageCrawler(IWebPageLoader webPageLoader)
        {
            _webPageLoader = webPageLoader;
        }

        public IWebPageCrawler<TNext> Navigate<TNext>(Uri url, IContentExtractStrategy<TNext> contentExtractStrategy)
        {
            return new WebPageCrawler<TNext>(_webPageLoader, url, contentExtractStrategy.ExtractData);
        }

        public IWebPageCrawler<TNext> Navigate<TNext>(Uri url, Func<string, Result<TNext>> contentExtractStrategy)
        {
            return new WebPageCrawler<TNext>(_webPageLoader, url, contentExtractStrategy);
        }
    }

    internal class WebPageCrawler<T> : IWebPageCrawler<T>
    {
        private readonly IWebPageLoader _webPageLoader;
        private readonly Func<string, Result<T>> _contentExtractStrategy;
        private readonly Uri _url;

        public WebPageCrawler(IWebPageLoader webPageLoader, Uri url, Func<string, Result<T>> contentExtractStrategy)
        {
            _url = url;
            _contentExtractStrategy = contentExtractStrategy;
            _webPageLoader = webPageLoader;
        }
        
        public IWebPageCrawler<TNext> Navigate<TNext>(Func<T, Uri> url, IContentExtractStrategy<TNext> contentExtractStrategy)
        {
            return Navigate(url, contentExtractStrategy.ExtractData);
        }

        public IWebPageCrawler<TNext> Navigate<TNext>(Func<T, Uri> url, Func<string, Result<TNext>> contentExtractStrategy)
        {
            return new WebPageCrawler<TNext, T>(_webPageLoader, url, contentExtractStrategy, this);
        }

        public async Task<Result<T>> GetResult()
        {
            var page = await _webPageLoader.Load(_url);
            var content = page.Bind(x => _contentExtractStrategy(x));

            return content;
        }
    }

    internal class WebPageCrawler<T, TPrevious> : IWebPageCrawler<T>
    {
        private readonly IWebPageLoader _webPageLoader;
        private readonly IWebPageCrawler<TPrevious> _previous;
        private readonly Func<string, Result<T>> _contentExtractStrategy;
        private readonly Func<TPrevious, Uri> _urlExtractor;

        public WebPageCrawler(
            IWebPageLoader webPageLoader, 
            Func<TPrevious, Uri> url, 
            Func<string, Result<T>> contentExtractStrategy, 
            IWebPageCrawler<TPrevious> previous)
        {
            _urlExtractor = url;
            _contentExtractStrategy = contentExtractStrategy;
            _previous = previous;
            _webPageLoader = webPageLoader;
        }

        public IWebPageCrawler<TNext> Navigate<TNext>(Func<T, Uri> urlExtractor, IContentExtractStrategy<TNext> contentExtractStrategy)
        {
            return Navigate(urlExtractor, contentExtractStrategy.ExtractData);
        }

        public IWebPageCrawler<TNext> Navigate<TNext>(Func<T, Uri> urlExtractor, Func<string, Result<TNext>> contentExtractStrategy)
        {
            return new WebPageCrawler<TNext, T>(_webPageLoader, urlExtractor, contentExtractStrategy, this);
        }

        public async Task<Result<T>> GetResult()
        {
            var previous = await _previous.GetResult();

            var result = await previous
                .Bind(x => _urlExtractor(x).Return())
                .Bind(async x => await _webPageLoader.Load(x));
 
            return result.Bind(_contentExtractStrategy);
        }
    }
}
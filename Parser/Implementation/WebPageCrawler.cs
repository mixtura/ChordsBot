//using System;
//using System.Threading.Tasks;
//using Parser.Interfaces;

//namespace Parser.Implementation
//{
//    internal class WebPageCrawler : IWebPageCrawler
//    {
//        private readonly IWebPageLoader _webPageLoader;

//        public WebPageCrawler(IWebPageLoader webPageLoader)
//        {
//            _webPageLoader = webPageLoader;
//        }

//        public IWebPageCrawler<T> Navigate<T>(string url, IContentExtractStrategy<T> contentExtractStrategy)
//        {
//            return Navigate(url, contentExtractStrategy.ExtractData);
//        }

//        public IWebPageCrawler<T> Navigate<T>(string url, Func<string, T> contentExtractStrategy)
//        {
//            var webPage = _webPageLoader.Load(new Uri(url));
            
//            return new WebPageCrawler<T>(_webPageLoader, async () => contentExtractStrategy(await webPage));
//        }
//    }

//    internal class WebPageCrawler<T> : WebPageCrawler, IWebPageCrawler<T>
//    {
//        private readonly Func<Task<T>> _previousResult;
//        private readonly IWebPageLoader _webPageLoader;

//        public WebPageCrawler(IWebPageLoader loader, Func<Task<T>> previousResult) 
//            : base(loader)
//        {
//            _webPageLoader = loader;
//            _previousResult = previousResult;
//        }

//        public IWebPageCrawler<T2> Navigate<T2>(Func<T, Uri> urlFn, IContentExtractStrategy<T2> contentExtractStrategy)
//        {
//            return Navigate(urlFn, contentExtractStrategy.ExtractData);
//        }

//        public IWebPageCrawler<T2> Navigate<T2>(Func<T, Uri> urlFn, Func<string, T2> contentExtractStrategy)
//        {
//            return new WebPageCrawler<T2>(_webPageLoader, async () =>
//            {
//                var previousResult = await _previousResult();
//                var url = urlFn(previousResult);

//                if (url == null || previousResult == null)
//                {
//                    return await Task.FromResult(default(T2));
//                }

//                var webPage = await _webPageLoader.Load(url);
//                return contentExtractStrategy(webPage);
//            });
//        }

//        public async Task<T> GetResult()
//        {
//            return await _previousResult();
//        }
//    }
//}
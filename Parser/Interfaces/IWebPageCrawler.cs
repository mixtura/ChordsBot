using System;
using System.Threading.Tasks;
using Parser.Common;

namespace Parser.Interfaces
{
    public interface IWebPageCrawler {}

    public interface IWebPageCrawler<T> : IWebPageCrawler
    {
        IWebPageCrawler<TNext> Navigate<TNext>(Func<T, Uri> url, IDataExtractor<TNext> dataExtractor);
        IWebPageCrawler<TNext> Navigate<TNext>(Func<T, Uri> url, Func<string, Result<TNext>> contentExtractStrategy);
        Task<Result<T>> GetResult();
    }
}
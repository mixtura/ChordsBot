using Parser.Common;

namespace Parser.Interfaces
{
    public interface IContentExtractStrategy<T>
    {
        Result<T> ExtractData(string content);
    }
}
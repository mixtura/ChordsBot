using Parser.Common;

namespace Parser.Interfaces
{
    public interface IDataExtractor<T>
    {
        Result<T> ExtractData(string content);
    }
}
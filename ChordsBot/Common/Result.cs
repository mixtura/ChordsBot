using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ChordsBot.Common
{
    public interface IResult<out T>
    {
        bool ContainsValue { get; }
        
        IResult<TO> Bind<TO>(Func<T, IResult<TO>> func);
        
        Task<IResult<TO>> Bind<TO>(Func<T, Task<IResult<TO>>> func);
        
        void MatchResult(Action<T> result);
        
        Task MatchResult(Func<T, Task> result);
        
        void MatchError(Action<string> error);
        
        Task MatchError(Func<string, Task> error);
        
        void Match(Action<T> result, Action<string> error);

        Task Match(Func<T, Task> result, Func<string, Task> error);
    }

    public class Result<T> : IResult<T>
    {
        private readonly T _value;
        private readonly string _errorMessage;

        public bool ContainsValue => _value != null && _errorMessage == null;

        public Result(T someValue)
        {
            _value = someValue == null ? throw new ArgumentNullException(nameof(someValue)) : someValue;
        }

        private Result(string errorMessage)
        {
            _errorMessage = errorMessage;
        }

        public IResult<TO> Bind<TO>(Func<T, IResult<TO>> func)
        {
            return ContainsValue ? func(_value) : Result<TO>.Error(_errorMessage);
        }

        public Task<IResult<TO>> Bind<TO>(Func<T, Task<IResult<TO>>> func)
        {
            return ContainsValue ? func(_value) : Task.FromResult(Result<TO>.Error(_errorMessage));
        }

        public void MatchResult(Action<T> result)
        {
            if (result == null) throw new ArgumentNullException(nameof(result));
            if (_value != null)
            {
                result(_value);
            }
        }

        public async Task MatchResult(Func<T, Task> result)
        {
            if (result == null) throw new ArgumentNullException(nameof(result));
            if (ContainsValue)
            {
                await result(_value);
            }
        }

        public void MatchError(Action<string> error)
        {            
            if (error == null) throw new ArgumentNullException(nameof(error));
            if (!ContainsValue)
            {
                error(_errorMessage);
            }
        }

        public async Task MatchError(Func<string, Task> error)
        {
            if (error == null) throw new ArgumentNullException(nameof(error));
            if (!ContainsValue)
            {
                await error(_errorMessage);
            }
        }

        public void Match(Action<T> result, Action<string> error)
        {         
            MatchResult(result);
            MatchError(error);
        }

        public async Task Match(Func<T, Task> result, Func<string, Task> error)
        {
            await MatchResult(result);
            await MatchError(error);
        }

        public override string ToString() 
        {
            return ContainsValue
                ? _value.ToString() 
                : _errorMessage;
        }

        public static IResult<T> Error(string errorMessage) => 
            errorMessage == null 
            ? throw new ArgumentNullException(nameof(errorMessage)) 
            : new Result<T>(errorMessage);
    }

    public static class ResultExtensions
    {
        public static IResult<T> Return<T>(this T value)
        {
            return value != null ? new Result<T>(value) : Result<T>.Error(string.Empty);
        }

        public static IResult<T> GetAsResult<TKey, T>(this IDictionary<TKey, T> dictionary, TKey key)
        {
            return key != null && dictionary.ContainsKey(key)
                ? dictionary[key].Return() 
                : Result<T>.Error("key not found");
        }

        public static IResult<T> GetByIndexAsResult<T>(this ICollection<T> collection, int index)
        {
            return collection.Count > index 
                ? collection.ElementAt(index).Return() 
                : Result<T>.Error("item not found");
        }

        public static IEnumerable<T> Unwrap<T>(this IResult<IEnumerable<T>> values)
        {
            var result = Enumerable.Empty<T>();

            values.MatchResult(x => result = x);

            return result;
        }
    }
}
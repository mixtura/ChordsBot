using System;
using System.Threading.Tasks;

namespace Parser.Common
{
    public class Result<T>
    {
        private readonly T _value;
        private readonly string _errorMessage;

        public Result(T someValue)
        {
            _value = someValue == null ? throw new ArgumentNullException(nameof(someValue)) : someValue;
        }

        private Result(string errorMessage)
        {
            _errorMessage = errorMessage;
        }

        public Result<TO> Bind<TO>(Func<T, Result<TO>> func)
        {
            return _value != null ? func(_value) : Result<TO>.Error(_errorMessage);
        }

        public async Task<Result<TO>> Bind<TO>(Func<T, Task<Result<TO>>> func)
        {
            return _value != null ? await func(_value) : await Task.FromResult(Result<TO>.Error(_errorMessage));
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
            if (_value != null)
            {
                await result(_value);
            }
        }

        public void MatchError(Action<string> error)
        {            
            if (error == null) throw new ArgumentNullException(nameof(error));
            if (_value == null)
            {
                error(_errorMessage);
            }
        }

        public async Task MatchError(Func<string, Task> error)
        {
            if (error == null) throw new ArgumentNullException(nameof(error));
            if (_value == null)
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
            return _value != null
                ? _value.ToString() 
                : _errorMessage;
        }

        public static Result<T> Error(string errorMessage) => 
            errorMessage == null 
            ? throw new ArgumentNullException(nameof(errorMessage)) 
            : new Result<T>(errorMessage);
    }

    public static class ResultExtensions
    {
        public static Result<T> Return<T>(this T value) where T : class
        {
            return value != null ? new Result<T>(value) : Result<T>.Error(string.Empty);
        }
    }
}
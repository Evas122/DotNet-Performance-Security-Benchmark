using System;

namespace SecPerf.Domain.Common
{
    public class Result
    {
        public bool IsSuccess { get; protected set; }
        public bool IsFailure => !IsSuccess;
        public Error? Error { get; protected set; }

        protected Result(bool isSuccess, Error? error)
        {
            IsSuccess = isSuccess;
            Error = error;
        }

        public static Result Ok() => new Result(true, null);
        public static Result Fail(string code, string message) => new Result(false, new Error(code, message));
        public static Result<T> Ok<T>(T value) => Result<T>.Success(value);
        public static Result<T> Fail<T>(string code, string message) => Result<T>.Failure(new Error(code, message));
    }

    public class Result<T> : Result
    {
        public T? Value { get; private set; }

        protected internal Result(T? value, bool isSuccess, Error? error) : base(isSuccess, error)
        {
            Value = value;
        }

        public static Result<T> Success(T value) => new Result<T>(value, true, null);
        public static Result<T> Failure(Error error) => new Result<T>(default, false, error);
    }
}

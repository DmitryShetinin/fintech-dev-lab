using Application.Common.Failures;


namespace Application.Common;


public sealed class Result<T>
{
  public bool IsSuccess { get; }

  public T? Value { get; }

  public IFailure? Error { get; }


  private Result(
      bool isSuccess,
      T? value,
      IFailure? error)
  {
    IsSuccess = isSuccess;
    Value = value;
    Error = error;
  }


  public static Result<T> Success(T value)
  {
    return new Result<T>(
        true,
        value,
        null);
  }


  public static Result<T> Failure(
      IFailure error)
  {
    return new Result<T>(
        false,
        default,
        error);
  }

  public bool Is<TFailure>() 
  where TFailure : class, IFailure
  {
    return Error is TFailure;
  }

  public TFailure? GetError<TFailure>() 
  where TFailure : class, IFailure
  {
    return Error as TFailure;
  }

}

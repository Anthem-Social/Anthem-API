namespace AnthemAPI.Common;

public class ServiceResult<T>
{
    public bool IsSuccess { get; private set; }
    public bool IsFailure => !IsSuccess;
    public T? Data { get; private set; }
    public Exception? Exception { get; private set; }
    public string? ErrorMessage { get; private set; }
    public string? ErrorOrigin { get; private set; }

    public ServiceResult(bool isSuccess, T? data, Exception? exception, string? errorMessage, string? errorOrigin)
    {
        IsSuccess = isSuccess;
        Data = data;
        Exception = exception;
        ErrorMessage = errorMessage;
        ErrorOrigin = errorOrigin;
    }

    public static ServiceResult<T> Success(T data)
    {
        // Console.WriteLine("Success: " + data);
        return new ServiceResult<T>(true, data, null, null, null);
    }

    public static ServiceResult<T> Failure(Exception? exception, string errorMessage, string errorOrigin)
    {
        Console.WriteLine("Failure: " + errorMessage);
        if (exception is not null)
        {
            Console.WriteLine("Exception: " + exception.Message);
        }
        Console.WriteLine("Origin: " + errorOrigin);
        // TODO: log error message and origin here to capture all failed service results
        return new ServiceResult<T>(false, default, exception, errorMessage, errorOrigin);
    }
}

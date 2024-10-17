namespace AnthemAPI.Common;

public class ServiceResult<T>
{
    public bool IsSuccess { get; private set; }
    public bool IsFailure => !IsSuccess;
    public T? Data { get; private set; }
    public string? ErrorMessage { get; private set; }
    public string? ErrorOrigin { get; private set; }

    public ServiceResult(bool isSuccess, T? data, string? errorMessage, string? errorOrigin)
    {
        IsSuccess = isSuccess;
        Data = data;
        ErrorMessage = errorMessage;
        ErrorOrigin = errorOrigin;
    }

    public static ServiceResult<T> Success(T data)
    {
        Console.WriteLine("Success: " + data);
        return new ServiceResult<T>(true, data, null, null);
    }

    public static ServiceResult<T> Failure(string errorMessage, string errorOrigin)
    {
        Console.WriteLine("Failure: " + errorMessage);
        // TODO: log error message and origin here to capture all failed service results
        return new ServiceResult<T>(false, default, errorMessage, errorOrigin);
    }
}

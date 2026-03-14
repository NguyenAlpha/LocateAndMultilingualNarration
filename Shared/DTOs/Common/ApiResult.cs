namespace Shared.DTOs.Common
{
    public class ApiResult<T>
    {
        public bool Success => Error == null;
        public T? Data { get; set; }
        public ErrorDetail? Error { get; set; }

        public static ApiResult<T> FromData(T data)
        {
            return new ApiResult<T> { Data = data };
        }

        public static ApiResult<T> FromError(ErrorDetail error)
        {
            return new ApiResult<T> { Error = error };
        }
    }
}

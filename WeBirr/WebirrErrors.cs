using System;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;

namespace WeBirr
{
    public static class WebirrErrors
    {
        const string StatusCodeDataKey = "WebirrStatusCode";

        public static bool IsTransient(Exception ex)
        {
            if (ex is HttpRequestException httpException)
            {
                return IsTransient(httpException);
            }

            if (ex is TaskCanceledException taskCanceledException)
            {
                return taskCanceledException.InnerException is TimeoutException ||
                    !taskCanceledException.CancellationToken.IsCancellationRequested;
            }

            if (ex is OperationCanceledException)
            {
                return false;
            }

            return false;
        }

        static bool IsTransient(HttpRequestException exception)
        {
            var statusCode = StatusCode(exception);
            if (!statusCode.HasValue)
            {
                return true;
            }

            return (int)statusCode.Value >= 500 ||
                (int)statusCode.Value == 429 ||
                statusCode.Value == HttpStatusCode.RequestTimeout;
        }

        static HttpStatusCode? StatusCode(HttpRequestException exception)
        {
            if (exception.Data.Contains(StatusCodeDataKey) &&
                exception.Data[StatusCodeDataKey] is int statusCodeValue)
            {
                return (HttpStatusCode)statusCodeValue;
            }

            var property = exception.GetType().GetProperty("StatusCode", BindingFlags.Instance | BindingFlags.Public);
            var value = property?.GetValue(exception);
            if (value is HttpStatusCode statusCode)
            {
                return statusCode;
            }
            return null;
        }
    }
}

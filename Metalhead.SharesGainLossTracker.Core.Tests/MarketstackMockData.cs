using System.Text;

namespace Metalhead.SharesGainLossTracker.Core.Tests;

internal static class MarketstackMockData
{
    internal static HttpResponseMessage CreateMarketstackAccessRestrictedHttpResponse()
    {
        return new HttpResponseMessage(System.Net.HttpStatusCode.Forbidden)
        {
            Content = new StringContent("{\"error\":{\"code\":\"function_access_restricted\",\"message\":\"Your current subscription plan does not support this API function\"}}", Encoding.UTF8, "application/json")
        };
    }

    internal static HttpResponseMessage CreateMarketstackRateLimitHttpResponse()
    {
        return new HttpResponseMessage(System.Net.HttpStatusCode.TooManyRequests)
        {
            Content = new StringContent("{\"error\":{\"code\":\"rate_limit_reached\",\"message\":\"You have exceeded the maximum rate limitation allowed on your subscription plan. Please refer to the \\\"Rate Limits\\\" section of the API Documentation for details.\"}}", Encoding.UTF8, "application/json")
        };
    }

    internal static HttpResponseMessage CreateMarketstackMonthlyLimitHttpResponse()
    {
        return new HttpResponseMessage(System.Net.HttpStatusCode.TooManyRequests)
        {
            Content = new StringContent("{\"error\":{\"code\":\"usage_limit_reached\",\"message\":\"Your monthly usage limit has been reached. Please upgrade your Subscription Plan.\"}}", Encoding.UTF8, "application/json")
        };
    }

    internal static HttpResponseMessage CreateMarketstackTooManyRequestsHttpResponse()
    {
        return new HttpResponseMessage(System.Net.HttpStatusCode.TooManyRequests)
        {
            Content = new StringContent("{\"error\":{\"code\":\"too_many_requests\",\"message\":\"\"}}", Encoding.UTF8, "application/json")
        };
    }

    internal static HttpResponseMessage CreateMarketstackInvalidEndpointHttpResponse()
    {
        return new HttpResponseMessage(System.Net.HttpStatusCode.NotFound)
        {
            Content = new StringContent("{\"error\":{\"code\":\"invalid_api_function\",\"message\":\"The API function you requested does not exist or is not supported.\"}}", Encoding.UTF8, "application/json")
        };
    }

    internal static HttpResponseMessage CreateMarketstackNoValidSymbolsHttpResponse()
    {
        return new HttpResponseMessage(System.Net.HttpStatusCode.UnprocessableContent)
        {
            Content = new StringContent("{\"error\":{\"code\":\"no_valid_symbols_provided\",\"message\":\"At least one valid symbol must be provided\"}}", Encoding.UTF8, "application/json")
        };
    }

    internal static HttpResponseMessage CreateMarketstackNotFoundHttpResponse()
    {
        return new HttpResponseMessage(System.Net.HttpStatusCode.NotFound)
        {
            Content = new StringContent("{\"error\":{\"code\":\"not_found_error\",\"message\":\"Route not found\"}}", Encoding.UTF8, "application/json")
        };
    }

    internal static HttpResponseMessage CreateMarketstackOtherErrorHttpResponse()
    {
        return new HttpResponseMessage(System.Net.HttpStatusCode.UnprocessableEntity)
        {
            Content = new StringContent("{\"error\":{\"code\":\"some-other-marketstack-error\",\"message\":\"\"}}", Encoding.UTF8, "application/json")
        };
    }

    internal static HttpResponseMessage CreateMarketstackDeserializingErrorHttpResponse()
    {
        return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
        {
            Content = new StringContent("{\"InvalidJson\":\"This is not a valid JSON response\"}", Encoding.UTF8, "application/json")
        };
    }
}

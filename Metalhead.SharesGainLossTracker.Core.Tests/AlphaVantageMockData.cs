namespace Metalhead.SharesGainLossTracker.Core.Tests;

internal static class AlphaVantageMockData
{
    internal static HttpResponseMessage CreateAlphaVantageAccessRestrictedHttpResponse()
    {
        return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
        {
            Content = new StringContent("{\"Information\":\"Thank you for using Alpha Vantage! This is a premium endpoint. You may subscribe to any of the premium plans at https://www.alphavantage.co/premium/ to instantly unlock all premium endpoints\"}", System.Text.Encoding.UTF8, "application/json")
        };
    }

    internal static HttpResponseMessage CreateAlphaVantageRateLimitHttpResponse()
    {
        return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
        {
            Content = new StringContent("{\"Note\":\"We have detected your API key as METALLICA666 and our standard API rate limit is 25 requests per day. Please visit https://www.alphavantage.co/premium/ if you would like to target a higher API call frequency.\"}", System.Text.Encoding.UTF8, "application/json")
        };
    }

    internal static HttpResponseMessage CreateAlphaVantageDailyLimitHttpResponse()
    {
        return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
        {
            Content = new StringContent("{\"Information\":\"We have detected your API key as METALLICA666 and our standard API rate limit is 25 requests per day. Please subscribe to any of the premium plans at https://www.alphavantage.co/premium/ to instantly remove all daily rate limits.\"}", System.Text.Encoding.UTF8, "application/json")
        };
    }

    internal static HttpResponseMessage CreateAlphaVantageInvalidEndpointHttpResponse()
    {
        return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
        {
            Content = new StringContent("{\"Error Message\":\"Invalid API call. Please retry or visit the documentation (https://www.alphavantage.co/documentation/) for TIME_SERIES_DAILY.\"}", System.Text.Encoding.UTF8, "application/json")
        };
    }

    internal static HttpResponseMessage CreateAlphaVantageDeserializingErrorHttpResponse()
    {
        return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
        {
            Content = new StringContent("{\"InvalidJson\":\"This is not a valid JSON response\"}", System.Text.Encoding.UTF8, "application/json")
        };
    }
}
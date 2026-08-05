using System.Net;
using Core.Enums;

public sealed class ProviderFailureClassifier
{
    public ProviderFailureReason Classify(
        HttpStatusCode statusCode)
    {
        return statusCode switch
        {
            HttpStatusCode.RequestTimeout
                => ProviderFailureReason.Timeout,

            HttpStatusCode.Unauthorized
                or HttpStatusCode.Forbidden
                => ProviderFailureReason.Unauthorized,


            HttpStatusCode.BadRequest
                or HttpStatusCode.UnprocessableEntity
                => ProviderFailureReason.Validation,


            HttpStatusCode.TooManyRequests
                or HttpStatusCode.InternalServerError
                or HttpStatusCode.BadGateway
                or HttpStatusCode.ServiceUnavailable
                or HttpStatusCode.GatewayTimeout
                => ProviderFailureReason.HttpTransient,


            _
                => ProviderFailureReason.HttpPermanent
        };
    }
}
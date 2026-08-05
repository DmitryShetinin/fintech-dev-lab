namespace Core.Enums;




public enum ProviderFailureReason
{
    None,

    Network,

    Timeout,

    Dns,

    HttpTransient,

    HttpPermanent,

    Unauthorized,

    Validation,

    Unknown
}
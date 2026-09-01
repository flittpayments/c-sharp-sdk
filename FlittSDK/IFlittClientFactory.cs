namespace FlittSDK
{
    /// <summary>
    /// Creates isolated Flitt clients for dynamic multi-merchant scenarios.
    /// </summary>
    public interface IFlittClientFactory
    {
        IFlittClient CreateClient(FlittClientOptions options);

        IFlittClient CreateClient(
            int merchantId,
            string secretKey,
            string creditKey = null
        );
    }
}

using System.Threading;
using System.Threading.Tasks;
using FlittSDK.Models;

namespace FlittSDK.Utils
{
    internal static class EndpointInvoker
    {
        internal static async Task<TResponse> InvokeAsync<TRequest, TResponse>(
            IFlittClient client,
            TRequest request,
            string actionUrl,
            bool isRoot = true,
            bool isCredit = false,
            CancellationToken cancellationToken = default(CancellationToken)
        ) where TResponse : ResponseV2
        {
            var response = await client.InvokeAsync<TRequest, TResponse>(
                request,
                actionUrl,
                isRoot,
                isCredit,
                cancellationToken
            ).ConfigureAwait(false);

            if (response.data != null && client.Protocol == "2.0")
            {
                return JsonFormatter.ConvertFromJson<TResponse>(response.data, true, "order");
            }

            return response;
        }
    }
}

using System;
using System.Globalization;
using FlittSDK.Models;
using Newtonsoft.Json;

namespace FlittSDK.Utils
{
    internal static class OrderAmounts
    {
        internal static int CaptureAmount(ResponseModel status)
        {
            var info = AdditionalInfo(status);
            return CheckedAmount(Parse(status.actual_amount) - (info.client_fee ?? 0));
        }

        internal static int ReverseAmount(ResponseModel status)
        {
            var info = AdditionalInfo(status);
            decimal actualAmount = Parse(status.actual_amount);
            decimal captureAmount = info.capture_amount ?? 0;
            decimal baseAmount = captureAmount == 0 ? actualAmount : captureAmount;
            return CheckedAmount(
                baseAmount - (info.client_fee ?? 0) - Parse(status.reversal_amount)
            );
        }

        internal static AdditionalInfo AdditionalInfo(ResponseModel status)
        {
            if (status == null || string.IsNullOrWhiteSpace(status.additional_info))
            {
                return new AdditionalInfo();
            }

            try
            {
                return JsonConvert.DeserializeObject<AdditionalInfo>(status.additional_info)
                       ?? new AdditionalInfo();
            }
            catch (JsonException exception)
            {
                throw new InvalidOperationException("Unable to parse additional_info", exception);
            }
        }

        private static decimal Parse(string value)
        {
            decimal result;
            return decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out result)
                ? result
                : 0;
        }

        private static int CheckedAmount(decimal value)
        {
            if (value < 0 || value > int.MaxValue || decimal.Truncate(value) != value)
            {
                throw new InvalidOperationException("Calculated order amount is outside the supported integer range");
            }

            return decimal.ToInt32(value);
        }
    }
}

using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace ABCRetailer.Functions.Functions;

public class QueueProcessorFunctions
{
    [Function("OrderNotifications_Processor")]
    public void OrderNotificationsProcessor(
        [QueueTrigger("order-notifications", Connection = "AzureStorage")]
        string message, FunctionContext ctx)
    {
        var log = ctx.GetLogger("OrderNotifications_Processor");
        log.LogInformation($"OrderNotifications message: {message}");
        // (Optional) write receipts, send emails, etc.
    }

    [Function("StockUpdates_Processor")]
    public void StockUpdatesProcessor(
        [QueueTrigger("stock-updates", Connection = "AzureStorage")]
        string message, FunctionContext ctx)
    {
        var log = ctx.GetLogger("StockUpdates_Processor");
        log.LogInformation($"StockUpdates message: {message}");
        // (Optional) sync to reporting DB, etc.
    }
}
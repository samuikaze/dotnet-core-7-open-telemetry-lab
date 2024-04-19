namespace OpenTelemetry.Lab.Api.Extensions
{
    internal static partial class LoggerExtensions
    {
        /// <summary>
        /// 啟動應用程式的日誌
        /// </summary>
        /// <param name="logger"></param>
        [LoggerMessage(LogLevel.Information, "Starting the app...")]
        public static partial void StartingApp(this ILogger logger);

        /// <summary>
        /// 取得天氣預報的日誌，其中包含請求的使用者名稱
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="userName">使用者名稱</param>
        [LoggerMessage(LogLevel.Information, "Requesting all weather forcast results, user name is `{userName}`.")]
        public static partial void RetrievingWeatherForecast(this ILogger logger, string userName);
    }
}

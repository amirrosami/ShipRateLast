namespace ShipRate.Application.Services.External
{
    public static class ApiIrConfig
    {
        public static string Token { get; set; } =
            Environment.GetEnvironmentVariable("APIIR_TOKEN") ?? "yzWPUOIcqZ4g6SNljAvJGGC7wmdt+XN52OmQ1eDj6qyi04zZddbCRLyxMKcd6Sc09HSifojLEH7iVKw1ErZy5OBsqsjX+1dhl7ig7KjdJRs=";
        public static int TimeoutSeconds { get; set; } = 30;
        public static bool SslVerify { get; set; } = true;
    }
}

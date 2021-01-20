namespace MultiFactor.SelfService.Windows.Portal.Services.API.DTO
{
    /// <summary>
    /// Generic API response
    /// </summary>
    public class ApiResponse
    {
        public bool Success { get; set; }

        public string Message { get; set; }

    }

    /// <summary>
    /// Api response with data
    /// </summary>
    public class ApiResponse<TModel> : ApiResponse
    {
        public TModel Model { get; set; }
    }
}
namespace MultiFactor.SelfService.Windows.Portal.Integrations.MultiFactorApi.Dto
{
    /// <summary>
    /// Generic Api response
    /// </summary>
    public class ApiResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }

        public ApiResponse(bool success, string message)
        {
            Success = success;
            Message = message;
        }

        public override string ToString()
        {
            return $"success: {Success}, message: {Message}";
        }
    }

    /// <summary>
    /// Api response with data
    /// </summary>
    public class ApiResponse<TModel> : ApiResponse
    {
        public TModel Model { get; set; }

        public ApiResponse(TModel model, bool success, string message)
            : base(success, message)
        {
            Model = model;
        }
    }
}

namespace BlenderParadise.Models
{
    public class ErrorViewModel
    {
        public string? RequestId { get; set; }
        public string? ExceptionMessage { get; set; }
        public string? StatusCode { get; set; }

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }
}
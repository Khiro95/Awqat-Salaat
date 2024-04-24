namespace AwqatSalaat.Services.AlAdhan
{
    internal class Response<T> where T : class
    {
        public int Code { get; set; }
        public string Status { get; set; }
        public T Data { get; set; }
    }
}

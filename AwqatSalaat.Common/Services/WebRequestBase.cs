namespace AwqatSalaat.Services
{
    public abstract class WebRequestBase : RequestBase, IWebRequest
    {
        public abstract string GetUrl();
    }
}

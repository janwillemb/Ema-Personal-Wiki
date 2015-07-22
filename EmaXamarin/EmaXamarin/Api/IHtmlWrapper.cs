namespace EmaXamarin.Api
{
    public interface IHtmlWrapper
    {
        string Wrap(string title, string contents);
        string ReplaceFileReferences(string html);
    }
}
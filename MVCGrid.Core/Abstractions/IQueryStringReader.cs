namespace MVCGrid.Abstractions
{
    /// <summary>
    /// Framework-neutral read access to the current request's query-string values,
    /// so query-string parsing (QueryStringParser) can live in the portable core.
    /// The web adapter implements this over HttpRequest.QueryString.
    ///
    /// The indexer returns null when a key is absent, matching HttpRequest.QueryString[key].
    /// </summary>
    public interface IQueryStringReader
    {
        string this[string key] { get; }
    }
}

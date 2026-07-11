namespace MVCGrid.Abstractions
{
    /// <summary>
    /// Framework-neutral replacement for System.Web.HttpResponse, passed to
    /// IMVCGridRenderingEngine.PrepareResponse so an engine can set content type,
    /// headers, and buffering before its output is written. The web adapter
    /// implements this over the host framework's response object.
    ///
    /// Members mirror the subset of HttpResponse that rendering engines actually
    /// use, so existing PrepareResponse method bodies compile unchanged.
    /// </summary>
    public interface IGridResponse
    {
        string ContentType { get; set; }
        bool BufferOutput { get; set; }
        void Clear();
        void AddHeader(string name, string value);
    }
}

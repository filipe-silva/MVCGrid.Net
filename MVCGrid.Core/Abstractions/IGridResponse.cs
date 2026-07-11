namespace MVCGrid.Abstractions
{
    /// <summary>
    /// Framework-neutral replacement for System.Web.HttpResponse, passed to
    /// IMVCGridRenderingEngine.PrepareResponse so an engine can set the content type
    /// and response headers before its output is written. The web adapter implements
    /// this over the host framework's response object.
    ///
    /// Deliberately limited to what maps cleanly across hosts: classic ASP.NET
    /// concepts like Clear() and BufferOutput are NOT here (ASP.NET Core's HttpResponse
    /// has no equivalent); the classic adapter applies those itself around the call.
    /// </summary>
    public interface IGridResponse
    {
        string ContentType { get; set; }
        void AddHeader(string name, string value);
    }
}

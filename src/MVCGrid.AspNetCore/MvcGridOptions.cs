namespace MVCGrid.AspNetCore
{
    /// <summary>
    /// Options for the ASP.NET Core MVCGrid adapter, configured via
    /// <c>services.AddMVCGrid(o =&gt; ...)</c>.
    /// </summary>
    public class MvcGridOptions
    {
        /// <summary>
        /// Base path the client script is served from and the AJAX/export requests go to.
        /// Map it with <c>app.MapMVCGrid()</c> and reference the script at
        /// <c>{HandlerPath}/script.js</c>. Default: <c>/mvcgrid</c>.
        /// </summary>
        public string HandlerPath { get; set; } = "/mvcgrid";

        /// <summary>
        /// When true, an AJAX data request that throws returns the exception detail to the
        /// client (the script shows it in place of the grid). Default: false.
        /// </summary>
        public bool ShowErrorDetails { get; set; } = false;
    }
}

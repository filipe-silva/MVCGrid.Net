namespace MVCGrid.Wasm
{
    /// <summary>
    /// Options for the browser/WASM MVCGrid host. Mirrors the ASP.NET Core adapter's
    /// options, minus anything server-specific.
    /// </summary>
    public class WasmGridOptions
    {
        /// <summary>
        /// Base path the generated HTML uses to reference the client script, icon images
        /// and the (intercepted) AJAX/export requests. Keep it <b>relative</b> (no leading
        /// slash) so the emitted <c>{HandlerPath}/sort.png</c> style URLs resolve correctly
        /// under a sub-path deployment such as GitHub Pages' <c>/demo/</c>. Default: <c>mvcgrid</c>.
        /// </summary>
        public string HandlerPath { get; set; } = "mvcgrid";

        /// <summary>
        /// When true, a render that throws surfaces the exception detail in place of the grid.
        /// Handy for a demo. Default: true.
        /// </summary>
        public bool ShowErrorDetails { get; set; } = true;
    }
}

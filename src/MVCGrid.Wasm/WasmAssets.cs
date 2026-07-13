using MVCGrid.Utility;

namespace MVCGrid.Wasm
{
    /// <summary>
    /// Emits the client assets embedded in MVCGrid.Core so the WASM host serves
    /// byte-identical script/icons and stays in sync with the core. The script is
    /// returned with its placeholder tokens substituted (same as the other adapters).
    /// </summary>
    internal static class WasmAssets
    {
        public static string GetClientScript(string handlerPath, bool showErrorDetails)
        {
            string js = EmbeddedResources.GetText("MVCGrid.js");
            js = js.Replace("%%HANDLERPATH%%", handlerPath);
            // Controller rendering mode is not supported client-side; controllerPath
            // resolves to the same base path (only used by that deferred mode).
            js = js.Replace("%%CONTROLLERPATH%%", handlerPath);
            js = js.Replace("%%ERRORDETAILS%%", showErrorDetails ? "true" : "false");
            return js;
        }

        public static byte[] GetImage(string fileName)
        {
            return EmbeddedResources.GetBinary(fileName);
        }
    }
}

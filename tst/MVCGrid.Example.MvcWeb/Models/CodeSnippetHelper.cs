namespace MVCGrid.Web.Models
{
    /// <summary>
    /// Kept for view compatibility: delegates to the shared CodeSnippets in
    /// MVCGrid.Example.Common, which slices the registration block for a grid
    /// out of its embedded SampleGrids.cs (no more MVCGridConfig.txt copy).
    /// Grids registered only in this host's MVCGridConfig.cs (e.g. CustomRazorView)
    /// are not in SampleGrids.cs and return an empty snippet — same as before.
    /// </summary>
    public class CodeSnippetHelper
    {
        public static string GetCodeSnippet(string gridName)
        {
            return Example.Common.CodeSnippets.For(gridName);
        }
    }
}

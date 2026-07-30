using System.Web;
using System.Web.Optimization;

namespace MVCGridExample
{
    public class BundleConfig
    {
        // Client-side libraries are restored by LibMan (libman.json) into lib/ at build time.
        public static void RegisterBundles(BundleCollection bundles)
        {
            bundles.Add(new ScriptBundle("~/bundles/jquery").Include(
                        "~/lib/jquery/jquery.js"));

            bundles.Add(new ScriptBundle("~/bundles/modernizr").Include(
                        "~/lib/modernizr/modernizr.js"));

            bundles.Add(new ScriptBundle("~/bundles/bootstrap").Include(
                      "~/lib/bootstrap/js/bootstrap.js",
                      "~/lib/respond/respond.js"));

            // CssRewriteUrlTransform keeps the ../fonts/glyphicons references working
            // when the bundle is served from its virtual path in optimized mode.
            bundles.Add(new StyleBundle("~/Content/css")
                      .Include("~/lib/bootstrap/css/bootstrap.css", new CssRewriteUrlTransform())
                      .Include("~/Content/site.css"));
        }
    }
}

namespace MVCGrid.Abstractions
{
    /// <summary>
    /// Framework-neutral replacement for System.Web.Mvc.UrlHelper. Column value
    /// expressions and templates receive one of these (as GridContext.UrlHelper /
    /// TemplateModel.Url) to build URLs without depending on System.Web.Mvc.
    ///
    /// The web adapter supplies an implementation backed by the host framework's
    /// URL generation. Only the Action overloads actually used by MVCGrid are
    /// declared here; add more only when a real caller needs them.
    /// </summary>
    public interface IMvcGridUrlBuilder
    {
        string Action(string actionName);
        string Action(string actionName, object routeValues);
        string Action(string actionName, string controllerName);
        string Action(string actionName, string controllerName, object routeValues);
    }
}

using MVCGrid.Abstractions;
using System.Web;
using System.Web.Mvc;

namespace MVCGrid.Web
{
    /// <summary>
    /// Adapts System.Web.Mvc.UrlHelper to the framework-neutral IMvcGridUrlBuilder
    /// used by the portable core (GridContext.UrlHelper / TemplateModel.Url).
    /// </summary>
    internal class MvcUrlBuilder : IMvcGridUrlBuilder
    {
        private readonly UrlHelper _url;

        public MvcUrlBuilder(UrlHelper url)
        {
            _url = url;
        }

        public string Action(string actionName)
        {
            return _url.Action(actionName);
        }

        public string Action(string actionName, object routeValues)
        {
            return _url.Action(actionName, routeValues);
        }

        public string Action(string actionName, string controllerName)
        {
            return _url.Action(actionName, controllerName);
        }

        public string Action(string actionName, string controllerName, object routeValues)
        {
            return _url.Action(actionName, controllerName, routeValues);
        }
    }

    /// <summary>
    /// Adapts System.Web.HttpResponse to the framework-neutral IGridResponse passed to
    /// IMVCGridRenderingEngine.PrepareResponse.
    /// </summary>
    internal class MvcGridResponse : IGridResponse
    {
        private readonly HttpResponse _response;

        public MvcGridResponse(HttpResponse response)
        {
            _response = response;
        }

        public string ContentType
        {
            get { return _response.ContentType; }
            set { _response.ContentType = value; }
        }

        public bool BufferOutput
        {
            get { return _response.BufferOutput; }
            set { _response.BufferOutput = value; }
        }

        public void Clear()
        {
            _response.Clear();
        }

        public void AddHeader(string name, string value)
        {
            _response.AddHeader(name, value);
        }
    }

    /// <summary>
    /// Adapts HttpRequest.QueryString to the framework-neutral IQueryStringReader used by
    /// the portable QueryStringParser.
    /// </summary>
    internal class MvcQueryStringReader : IQueryStringReader
    {
        private readonly HttpRequest _request;

        public MvcQueryStringReader(HttpRequest request)
        {
            _request = request;
        }

        public string this[string key]
        {
            get { return _request.QueryString[key]; }
        }
    }
}

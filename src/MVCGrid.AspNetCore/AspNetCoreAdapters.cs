using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MVCGrid.Abstractions;

namespace MVCGrid.AspNetCore
{
    /// <summary>Adapts ASP.NET Core's IUrlHelper to the core's IMvcGridUrlBuilder.</summary>
    internal sealed class AspNetCoreUrlBuilder : IMvcGridUrlBuilder
    {
        private readonly IUrlHelper _url;

        public AspNetCoreUrlBuilder(IUrlHelper url)
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

    /// <summary>Adapts ASP.NET Core's HttpResponse to the core's IGridResponse.</summary>
    internal sealed class AspNetCoreGridResponse : IGridResponse
    {
        private readonly HttpResponse _response;

        public AspNetCoreGridResponse(HttpResponse response)
        {
            _response = response;
        }

        public string ContentType
        {
            get { return _response.ContentType; }
            set { _response.ContentType = value; }
        }

        public void AddHeader(string name, string value)
        {
            _response.Headers[name] = value;
        }
    }

    /// <summary>
    /// Adapts ASP.NET Core's request query collection to the core's IQueryStringReader.
    /// Returns null for an absent key (matching the classic HttpRequest.QueryString[key]).
    /// </summary>
    internal sealed class AspNetCoreQueryStringReader : IQueryStringReader
    {
        private readonly HttpRequest _request;

        public AspNetCoreQueryStringReader(HttpRequest request)
        {
            _request = request;
        }

        public string this[string key]
        {
            get { return _request.Query.ContainsKey(key) ? _request.Query[key].ToString() : null; }
        }
    }
}

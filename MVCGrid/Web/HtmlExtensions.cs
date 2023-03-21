using MVCGrid.Engine;
using MVCGrid.Interfaces;
using MVCGrid.Models;
using MVCGrid.Rendering;
using MVCGrid.Utility;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace MVCGrid.Web
{
    /// <summary>
    /// Extensions for 'System.Web.Mvc.HtmlHelper' to be used on Razor pages that allow getting the MVCGrid.
    /// </summary>
    public static class HtmlExtensions
    {
        /// <summary>
        /// Returns a generated HTML based on the name variable, where the name is the MVCGrid name.
        /// </summary>
        /// <param name="helper"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static IHtmlString MVCGrid(this HtmlHelper helper, string name)
        {
            return MVCGrid(helper, name, new Dictionary<string, string>());
        }

        /// <summary>
        /// Receives pagaParameters as an anonymous type.
        /// </summary>
        /// <param name="helper"></param>
        /// <param name="name"></param>
        /// <param name="pageParameters"></param>
        /// <returns></returns>
        public static IHtmlString MVCGrid(this HtmlHelper helper, string name, object pageParameters)
        {
            Dictionary<string, string> pageParamsDict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (pageParameters != null)
            {
                foreach (PropertyDescriptor descriptor in TypeDescriptor.GetProperties(pageParameters))
                {
                    object obj2 = descriptor.GetValue(pageParameters);
                    pageParamsDict.Add(descriptor.Name, obj2.ToString());
                }
            }
            return MVCGrid(helper, name, pageParamsDict);
        }

        /// <summary>
        /// Receives pageParameters as a Dictionary.
        /// </summary>
        /// <param name="helper"></param>
        /// <param name="name"></param>
        /// <param name="pageParameters"></param>
        /// <returns></returns>
        public static IHtmlString MVCGrid(this HtmlHelper helper, string name, Dictionary<string, object> pageParameters)
        {
            Dictionary<string, string> pageParamsDict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (pageParameters != null)
            {
                foreach (var item in pageParameters)
                {
                    if (item.Value == null)
                        pageParamsDict.Add(item.Key, item.Value as string);
                    else
                        pageParamsDict.Add(item.Key, item.Value.ToString());
                }
            }
            return MVCGrid(helper, name, pageParamsDict);
        }

        internal static IHtmlString MVCGrid(this HtmlHelper helper, string name, Dictionary<string, string> pageParameters)
        {
            var grid = MVCGridDefinitionTable.GetDefinitionInterface(name);
            var ge = new GridEngine();
            string html = ge.GetBasePageHtml(helper, name, grid, pageParameters);
            return MvcHtmlString.Create(html);
        }
    }
}

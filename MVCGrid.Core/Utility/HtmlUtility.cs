using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MVCGrid.Utility
{
    public class HtmlUtility
    {
        public const string ContainerCssClass = "MVCGridContainer";

        public static string GetContainerHtmlId(string name)
        {
            return String.Format("MVCGridContainer_{0}", name);
        }

        public static string GetTableHolderHtmlId(string name)
        {
            return String.Format("MVCGridTableHolder_{0}", name);
        }

        public static string GetTableHtmlId(string name)
        {
            return String.Format("MVCGridTable_{0}", name);
        }

        public static string MakeCssClassStirng(HashSet<string> classes)
        {
            StringBuilder sb = new StringBuilder();

            foreach (var c in classes)
            {
                if (sb.Length > 0)
                {
                    sb.Append(" ");
                }
                sb.Append(c);
            }
            return sb.ToString();
        }

        public static string MakeCssClassAttributeStirng(HashSet<string> classes)
        {
            if (classes == null || classes.Count == 0)
                return "";

            return String.Format(" class='{0}'", MakeCssClassStirng(classes));
        }

        public static string MakeCssClassAttributeStirng(string classString)
        {
            if (String.IsNullOrWhiteSpace(classString))
                return "";

            return String.Format(" class='{0}'", classString);
        }

        public static string MakeGotoPageLink(string gridName, int pageNum)
        {
            return String.Format("MVCGrid.setPage(\"{0}\", {1}); return false;", gridName, pageNum);
        }

        public static string MakeSortLink(string gridName, string columnName, MVCGrid.Models.SortDirection direction)
        {
            return String.Format("MVCGrid.setSort(\"{0}\", \"{1}\", \"{2}\"); return false;", gridName, columnName, direction.ToString());
        }

        /// <summary>
        /// Portable replacement for System.Web.HttpUtility.JavaScriptStringEncode (without
        /// surrounding quotes). Escapes the same characters the framework version does so
        /// that values embedded in the client-side JSON blocks are safe.
        /// </summary>
        public static string JavaScriptStringEncode(string value)
        {
            if (String.IsNullOrEmpty(value))
            {
                return "";
            }

            StringBuilder sb = new StringBuilder(value.Length + 8);
            foreach (char c in value)
            {
                switch (c)
                {
                    case '\\': sb.Append("\\\\"); break;
                    case '"': sb.Append("\\\""); break;
                    case '\b': sb.Append("\\b"); break;
                    case '\f': sb.Append("\\f"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    case '<': sb.Append("\\u003c"); break;
                    case '>': sb.Append("\\u003e"); break;
                    case '&': sb.Append("\\u0026"); break;
                    case '\'': sb.Append("\\u0027"); break;
                    default:
                        if (c < ' ')
                        {
                            sb.Append("\\u");
                            sb.Append(((int)c).ToString("x4"));
                        }
                        else
                        {
                            sb.Append(c);
                        }
                        break;
                }
            }
            return sb.ToString();
        }
    }
}

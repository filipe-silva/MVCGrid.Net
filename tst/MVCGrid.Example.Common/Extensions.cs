using System;
using System.Linq;
using System.Linq.Expressions;
using MVCGrid.Models;

namespace MVCGrid.Example.Common
{
    public static class Extensions
    {
        /// <summary>OrderBy overload that takes an MVCGrid SortDirection (used by the direct-query grids).</summary>
        public static IOrderedQueryable<TSource> OrderBy<TSource, TKey>(
            this IQueryable<TSource> source,
            Expression<Func<TSource, TKey>> keySelector,
            SortDirection order)
        {
            switch (order)
            {
                case SortDirection.Unspecified:
                case SortDirection.Asc:
                    return source.OrderBy(keySelector);
                case SortDirection.Dsc:
                    return source.OrderByDescending(keySelector);
            }
            throw new ArgumentOutOfRangeException(nameof(order));
        }
    }
}

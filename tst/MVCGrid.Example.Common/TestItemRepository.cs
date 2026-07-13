using System;
using System.Collections.Generic;
using System.Linq;

namespace MVCGrid.Example.Common
{
    public class TestItem
    {
        public string Col1 { get; set; }
        public string Col2 { get; set; }
        public string Col3 { get; set; }
        public bool Col4 { get; set; }
    }

    /// <summary>Second grid of the "multiple grids" demo. Generated once into a static cache.</summary>
    public class TestItemRepository
    {
        private static readonly Lazy<List<TestItem>> Cache = new Lazy<List<TestItem>>(Build);

        private static List<TestItem> Build()
        {
            var rng = new Random(20240114);
            var items = new List<TestItem>();
            for (int i = 1; i < 1087; i++)
            {
                items.Add(new TestItem
                {
                    Col1 = "Row" + i,
                    Col2 = RandomString(rng, 8),
                    Col3 = RandomString(rng, 11),
                    Col4 = rng.Next(100) % 2 == 0
                });
            }
            return items;
        }

        public IEnumerable<TestItem> GetData(out int totalRecords, int? limitOffset, int? limitRowCount, string orderBy, bool desc)
        {
            return GetData(out totalRecords, null, limitOffset, limitRowCount, orderBy, desc);
        }

        public IEnumerable<TestItem> GetData(out int totalRecords, string col3Filter, int? limitOffset, int? limitRowCount, string orderBy, bool desc)
        {
            var q = Cache.Value.AsQueryable();

            if (!String.IsNullOrWhiteSpace(orderBy))
            {
                switch (orderBy.ToLower())
                {
                    case "col1": q = desc ? q.OrderByDescending(p => p.Col1) : q.OrderBy(p => p.Col1); break;
                    case "col2": q = desc ? q.OrderByDescending(p => p.Col2) : q.OrderBy(p => p.Col2); break;
                    case "col3": q = desc ? q.OrderByDescending(p => p.Col3) : q.OrderBy(p => p.Col3); break;
                }
            }

            if (!String.IsNullOrWhiteSpace(col3Filter))
            {
                q = q.Where(p => p.Col3.IndexOf(col3Filter, StringComparison.OrdinalIgnoreCase) >= 0);
            }

            totalRecords = q.Count();

            if (limitOffset.HasValue)
            {
                q = q.Skip(limitOffset.Value).Take(limitRowCount.Value);
            }

            return q.ToList();
        }

        private const string Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

        private static string RandomString(Random rng, int size)
        {
            var buffer = new char[size];
            for (int i = 0; i < size; i++)
            {
                buffer[i] = Chars[rng.Next(Chars.Length)];
            }
            return new string(buffer);
        }
    }
}

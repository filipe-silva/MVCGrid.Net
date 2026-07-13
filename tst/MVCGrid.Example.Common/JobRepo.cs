using System;
using System.Collections.Generic;
using System.Linq;

namespace MVCGrid.Example.Common
{
    public class Job
    {
        public int JobId { get; set; }
        public string Name { get; set; }
        public Contact Contact { get; set; }
    }

    public class Contact
    {
        public int Id { get; set; }
        public string FullName { get; set; }
    }

    /// <summary>Nested-object demo data. Generated once into a static cache (was HttpContext.Cache).</summary>
    public class JobRepo
    {
        private static readonly Lazy<List<Job>> Cache = new Lazy<List<Job>>(Build);

        private static List<Job> Build()
        {
            var rng = new Random(20240113);
            var items = new List<Job>();
            int contactId = 0;
            for (int i = 1; i < 1087; i++)
            {
                var j = new Job { JobId = i, Name = RandomString(rng, 10) };
                if (rng.NextDouble() > 0.5)
                {
                    contactId++;
                    j.Contact = new Contact { Id = contactId, FullName = RandomString(rng, 5) };
                }
                items.Add(j);
            }
            return items;
        }

        public IEnumerable<Job> GetData(out int totalRecords, string globalSearch, int? limitOffset, int? limitRowCount, string orderBy, bool desc)
        {
            var q = Cache.Value.AsQueryable();

            if (!String.IsNullOrWhiteSpace(globalSearch))
            {
                q = q.Where(p => p.Name.IndexOf(globalSearch, StringComparison.OrdinalIgnoreCase) >= 0);
            }

            totalRecords = q.Count();

            if (!String.IsNullOrWhiteSpace(orderBy))
            {
                switch (orderBy.ToLower())
                {
                    case "id": q = desc ? q.OrderByDescending(p => p.JobId) : q.OrderBy(p => p.JobId); break;
                    case "name": q = desc ? q.OrderByDescending(p => p.Name) : q.OrderBy(p => p.Name); break;
                    case "contact": q = desc ? q.OrderByDescending(p => p.Contact.FullName) : q.OrderBy(p => p.Contact.FullName); break;
                }
            }

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

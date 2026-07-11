namespace MVCGrid.AspNetCoreExample.Data
{
    /// <summary>In-memory sample data + a query method honouring filter/sort/paging.</summary>
    public static class PeopleRepository
    {
        private static readonly List<Person> All = Build();

        private static List<Person> Build()
        {
            var first = new[] { "Patricia", "Jeffrey", "Dorothy", "Kathryn", "Evelyn", "Michael", "Nancy", "Joyce", "Martin", "Lois" };
            var last = new[] { "Jacobs", "Shaw", "Nguyen", "Cruz", "Wilson", "Collins", "Crawford", "Frazier", "Snyder", "Reed" };

            var list = new List<Person>();
            var seed = new DateTime(2012, 1, 1);
            for (int i = 1; i <= 200; i++)
            {
                list.Add(new Person
                {
                    Id = i,
                    FirstName = first[i % first.Length],
                    LastName = last[(i / first.Length) % last.Length],
                    StartDate = seed.AddDays(i * 7),
                    Active = (i % 3 != 0),
                    Gender = (i % 2 == 0) ? "Male" : "Female"
                });
            }
            return list;
        }

        public static (List<Person> Items, int Total) GetData(
            string firstName, string lastName, string sortColumn, bool desc, int? offset, int? rowCount)
        {
            IEnumerable<Person> q = All;

            if (!string.IsNullOrWhiteSpace(firstName))
                q = q.Where(p => p.FirstName.Contains(firstName, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(lastName))
                q = q.Where(p => p.LastName.Contains(lastName, StringComparison.OrdinalIgnoreCase));

            switch ((sortColumn ?? "").ToLowerInvariant())
            {
                case "firstname": q = desc ? q.OrderByDescending(p => p.FirstName) : q.OrderBy(p => p.FirstName); break;
                case "lastname": q = desc ? q.OrderByDescending(p => p.LastName) : q.OrderBy(p => p.LastName); break;
                case "startdate": q = desc ? q.OrderByDescending(p => p.StartDate) : q.OrderBy(p => p.StartDate); break;
                case "active": q = desc ? q.OrderByDescending(p => p.Active) : q.OrderBy(p => p.Active); break;
                default: q = desc ? q.OrderByDescending(p => p.Id) : q.OrderBy(p => p.Id); break;
            }

            int total = q.Count();
            if (offset.HasValue) q = q.Skip(offset.Value);
            if (rowCount.HasValue) q = q.Take(rowCount.Value);

            return (q.ToList(), total);
        }
    }
}

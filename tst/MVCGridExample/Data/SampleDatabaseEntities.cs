using System;
using System.Collections.Generic;
using System.Linq;

namespace MVCGrid.Web.Data
{
    /// <summary>
    /// In-memory stand-in for the old Entity Framework context. It keeps the same
    /// shape the demo grids already expect — a disposable object exposing
    /// <see cref="People"/> as an <see cref="IQueryable{T}"/> — so no grid code had to
    /// change, but it drops the SQL Server / LocalDB (.mdf) dependency entirely.
    /// The sample rows are generated deterministically once and shared read-only.
    /// </summary>
    public class SampleDatabaseEntities : IDisposable
    {
        private static readonly Person[] SeedData = BuildSeedData();

        public IQueryable<Person> People
        {
            get { return SeedData.AsQueryable(); }
        }

        public void Dispose()
        {
            // Nothing to dispose; kept so existing `using (var db = ...)` blocks compile unchanged.
        }

        private static Person[] BuildSeedData()
        {
            var firstNames = new[]
            {
                "Patricia", "Jeffrey", "Dorothy", "Kathryn", "Evelyn", "Michael", "Nancy",
                "Joyce", "Martin", "Lois", "Harold", "Diane", "Roger", "Theresa", "Bruce",
                "Gloria", "Philip", "Jean", "Bobby", "Rachel"
            };
            var lastNames = new[]
            {
                "Jacobs", "Shaw", "Nguyen", "Cruz", "Wilson", "Collins", "Crawford", "Frazier",
                "Snyder", "Reed", "Hughes", "Foster", "Bryant", "Palmer", "Sullivan", "Russell",
                "Griffin", "Perry", "Butler", "Barnes"
            };
            var genders = new[] { "Female", "Male" };

            var startSeed = new DateTime(2012, 1, 1);
            var people = new List<Person>(capacity: 200);
            for (int i = 1; i <= 200; i++)
            {
                var first = firstNames[i % firstNames.Length];
                var last = lastNames[(i / firstNames.Length) % lastNames.Length];
                people.Add(new Person
                {
                    Id = i,
                    FirstName = first,
                    LastName = last,
                    Email = string.Format("{0}.{1}{2}@example.com", first.ToLowerInvariant(), last.ToLowerInvariant(), i),
                    Gender = genders[i % genders.Length],
                    Active = (i % 3 != 0),
                    Employee = (i % 4 != 0),
                    StartDate = startSeed.AddDays(i * 7)
                });
            }
            return people.ToArray();
        }
    }
}

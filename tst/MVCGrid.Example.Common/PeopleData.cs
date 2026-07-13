using System;
using System.Collections.Generic;
using System.Linq;

namespace MVCGrid.Example.Common
{
    /// <summary>
    /// The shared in-memory people dataset (200 deterministic rows). Replaces the classic
    /// example's Entity Framework / LocalDB source with a portable static list that runs on
    /// any host, including WebAssembly.
    /// </summary>
    public static class PeopleData
    {
        private static readonly Person[] Seed = Build();

        /// <summary>All rows as an IQueryable (LINQ-to-objects), for grids that query directly.</summary>
        public static IQueryable<Person> Query()
        {
            return Seed.AsQueryable();
        }

        private static Person[] Build()
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
                string first = firstNames[i % firstNames.Length];
                string last = lastNames[(i / firstNames.Length) % lastNames.Length];
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

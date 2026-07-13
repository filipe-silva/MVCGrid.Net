using System;
using System.Collections.Generic;
using System.Linq;

namespace MVCGrid.Example.Common
{
    /// <summary>
    /// Query API over the shared in-memory people data. Kept as an interface + implementation
    /// so the classic host can still demo dependency injection, but the shared grid catalog
    /// uses it directly (no DI container needed on any host).
    /// </summary>
    public interface IPeopleRepository
    {
        IEnumerable<Person> GetData(out int totalRecords, string globalSearch, int? limitOffset, int? limitRowCount, string orderBy, bool desc);
        IEnumerable<Person> GetData(out int totalRecords, int? limitOffset, int? limitRowCount, string orderBy, bool desc);
        IEnumerable<Person> GetData(out int totalRecords, string filterFirstName, string filterLastName, bool? filterActive, int? limitOffset, int? limitRowCount, string orderBy, bool desc);
    }

    public class PeopleRepository : IPeopleRepository
    {
        public IEnumerable<Person> GetData(out int totalRecords, string filterFirstName, string filterLastName, bool? filterActive, int? limitOffset, int? limitRowCount, string orderBy, bool desc)
        {
            return GetData(out totalRecords, null, filterFirstName, filterLastName, filterActive, limitOffset, limitRowCount, orderBy, desc);
        }

        public IEnumerable<Person> GetData(out int totalRecords, string globalSearch, int? limitOffset, int? limitRowCount, string orderBy, bool desc)
        {
            return GetData(out totalRecords, globalSearch, null, null, null, limitOffset, limitRowCount, orderBy, desc);
        }

        public IEnumerable<Person> GetData(out int totalRecords, int? limitOffset, int? limitRowCount, string orderBy, bool desc)
        {
            return GetData(out totalRecords, null, null, null, null, limitOffset, limitRowCount, orderBy, desc);
        }

        private IEnumerable<Person> GetData(out int totalRecords, string globalSearch, string filterFirstName, string filterLastName, bool? filterActive, int? limitOffset, int? limitRowCount, string orderBy, bool desc)
        {
            var query = PeopleData.Query();

            // Case-insensitive Contains (LINQ-to-objects string.Contains is ordinal, and
            // netstandard2.0 lacks the StringComparison overload — use IndexOf).
            if (!String.IsNullOrWhiteSpace(filterFirstName))
            {
                query = query.Where(p => p.FirstName.IndexOf(filterFirstName, StringComparison.OrdinalIgnoreCase) >= 0);
            }
            if (!String.IsNullOrWhiteSpace(filterLastName))
            {
                query = query.Where(p => p.LastName.IndexOf(filterLastName, StringComparison.OrdinalIgnoreCase) >= 0);
            }
            if (filterActive.HasValue)
            {
                query = query.Where(p => p.Active == filterActive.Value);
            }
            if (!String.IsNullOrWhiteSpace(globalSearch))
            {
                query = query.Where(p => (p.FirstName + " " + p.LastName).IndexOf(globalSearch, StringComparison.OrdinalIgnoreCase) >= 0);
            }

            totalRecords = query.Count();

            if (!String.IsNullOrWhiteSpace(orderBy))
            {
                switch (orderBy.ToLower())
                {
                    case "firstname": query = desc ? query.OrderByDescending(p => p.FirstName) : query.OrderBy(p => p.FirstName); break;
                    case "lastname": query = desc ? query.OrderByDescending(p => p.LastName) : query.OrderBy(p => p.LastName); break;
                    case "active": query = desc ? query.OrderByDescending(p => p.Active) : query.OrderBy(p => p.Active); break;
                    case "email": query = desc ? query.OrderByDescending(p => p.Email) : query.OrderBy(p => p.Email); break;
                    case "gender": query = desc ? query.OrderByDescending(p => p.Gender) : query.OrderBy(p => p.Gender); break;
                    case "id": query = desc ? query.OrderByDescending(p => p.Id) : query.OrderBy(p => p.Id); break;
                    case "startdate": query = desc ? query.OrderByDescending(p => p.StartDate) : query.OrderBy(p => p.StartDate); break;
                }
            }

            if (limitOffset.HasValue)
            {
                query = query.Skip(limitOffset.Value).Take(limitRowCount.Value);
            }

            return query.ToList();
        }
    }
}

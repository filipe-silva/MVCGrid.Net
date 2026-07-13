using System;

namespace MVCGrid.Example.Common
{
    /// <summary>Sample row model shared by every example host (superset of all demo columns).</summary>
    public class Person
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Gender { get; set; }
        public bool Active { get; set; }
        public bool Employee { get; set; }
        public DateTime? StartDate { get; set; }
    }
}

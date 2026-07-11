namespace MVCGrid.AspNetCoreExample.Data
{
    public class Person
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public DateTime? StartDate { get; set; }
        public bool Active { get; set; }
        public string Gender { get; set; }
    }
}

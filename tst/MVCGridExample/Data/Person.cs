namespace MVCGrid.Web.Data
{
    /// <summary>Sample row model used by the demo grids (backed by in-memory data).</summary>
    public partial class Person
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Gender { get; set; }
        public bool Active { get; set; }
        public bool Employee { get; set; }
        public System.DateTime? StartDate { get; set; }
    }
}

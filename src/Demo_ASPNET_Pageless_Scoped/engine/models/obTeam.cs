namespace System.models
{
    /// <summary>Maps to the `team` table.</summary>
    public class obTeam
    {
        int id = 0;
        string code = "";
        string name = "";
        string city = "";
        int status = 0;

        public int Id { get { return id; } set { id = value; } }
        public string Code { get { return code; } set { code = value; } }
        public string Name { get { return name; } set { name = value; } }
        public string City { get { return city; } set { city = value; } }
        public int Status { get { return status; } set { status = value; } }
    }
}

namespace System.models
{
    /// <summary>
    /// Maps to the `player` table. Private fields match column names;
    /// public properties follow C# PascalCase conventions. MySqlExpress
    /// binds into the private fields automatically.
    /// </summary>
    public class obPlayer
    {
        int id = 0;
        string code = "";
        string name = "";
        DateTime date_register = DateTime.MinValue;
        string tel = "";
        string email = "";
        int status = 0;

        public int Id { get { return id; } set { id = value; } }
        public string Code { get { return code; } set { code = value; } }
        public string Name { get { return name; } set { name = value; } }
        public DateTime DateRegister { get { return date_register; } set { date_register = value; } }
        public string Tel { get { return tel; } set { tel = value; } }
        public string Email { get { return email; } set { email = value; } }
        public int Status { get { return status; } set { status = value; } }
    }
}

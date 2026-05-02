namespace System.models
{
    /// <summary>
    /// Maps to the JOIN result from
    /// `player a inner join player_team b on a.id = b.player_id
    ///                inner join team c on b.team_id = c.id`.
    ///
    /// This is the "custom projection" POCO pattern — field names match
    /// the column aliases in the SELECT, and MySqlExpress binds each
    /// column into a matching field by name.
    /// </summary>
    public class obRosterRow
    {
        int id = 0;
        string code = "";
        string name = "";
        int year = 0;
        decimal score = 0m;
        int level = 0;
        string teamname = "";
        string teamcode = "";
        int teamid = 0;

        public int Id { get { return id; } set { id = value; } }
        public string Code { get { return code; } set { code = value; } }
        public string Name { get { return name; } set { name = value; } }
        public int Year { get { return year; } set { year = value; } }
        public decimal Score { get { return score; } set { score = value; } }
        public int Level { get { return level; } set { level = value; } }
        public string Teamname { get { return teamname; } set { teamname = value; } }
        public string Teamcode { get { return teamcode; } set { teamcode = value; } }
        public int Teamid { get { return teamid; } set { teamid = value; } }
    }
}

using MySqlConnector;

namespace System
{
    /// <summary>
    /// Opens a MySQL connection + command + MySqlExpress instance, hands
    /// them to a delegate, and disposes everything on exit.
    ///
    /// Every handler in this demo uses Db.Run or Db.Get; no handler
    /// manages connection lifecycle itself. This keeps the body of each
    /// handler focused on SQL and markup, which is the point.
    /// </summary>
    public static class Db
    {
        /// <summary>
        /// Runs a void action that needs a MySqlExpress instance.
        /// </summary>
        public static void Run(Action<MySqlExpress> action)
        {
            using (MySqlConnection conn = new MySqlConnection(Config.ConnString))
            {
                conn.Open();
                using (MySqlCommand cmd = new MySqlCommand())
                {
                    cmd.Connection = conn;
                    MySqlExpress m = new MySqlExpress(cmd);
                    action(m);
                }
            }
        }

        /// <summary>
        /// Runs a function that returns a value using a MySqlExpress instance.
        /// </summary>
        public static T Get<T>(Func<MySqlExpress, T> func)
        {
            using (MySqlConnection conn = new MySqlConnection(Config.ConnString))
            {
                conn.Open();
                using (MySqlCommand cmd = new MySqlCommand())
                {
                    cmd.Connection = conn;
                    MySqlExpress m = new MySqlExpress(cmd);
                    return func(m);
                }
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MySqlExpress_Helper
{
    [Serializable]
    public class settings
    {
        public string ConnStr = "";
        public string CustomSql = "";
        public int FieldType = 0;
        public Point Location= new Point();
        public Size FormSize = new Size();
    }
}

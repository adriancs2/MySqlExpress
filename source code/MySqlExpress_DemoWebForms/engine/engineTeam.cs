using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;

namespace System
{
    public class engineTeam
    {
        public static void SaveLogo(MySqlExpress m, int id, byte[] fileBytes)
        {
            string folder = HttpContext.Current.Server.MapPath("~/teamlogo");

            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            Dictionary<string, object> dicParam = new Dictionary<string, object>();
            dicParam["id"] = id;

            int logoid = m.ExecuteScalar<int>("select logo_id from team where id=@id;", dicParam);

            logoid++;

            MemoryStream ms = new MemoryStream(fileBytes);
            System.Drawing.Image img = System.Drawing.Image.FromStream(ms);
            img = ImageFunc.ResizeCropFitToSize(img, 100, 100);

            string filePath = HttpContext.Current.Server.MapPath($"~/teamlogo/{id}-{logoid}.png");
            img.Save(filePath);

            dicParam.Clear();
            dicParam["@logoid"] = logoid;
            dicParam["@id"] = id;

            m.Execute("update team set logo_id=@logoid where id=@id;", dicParam);
        }
    }
}
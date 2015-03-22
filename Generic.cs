using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml.Serialization;
using System.IO;
using System.Xml;

namespace TerraFirma
{
    public class Generic<T>
    {
        public static void SaveToDisk(T container, string filename)
        {

            XmlSerializer x = new XmlSerializer(typeof(T));

            TextWriter writer = new StreamWriter(filename);

            x.Serialize(writer, container);

            writer.Close();

        }

        public static bool LoadFromDisk(out T _container, string filename)
        {
            // Read the XML file if it exists ---

            FileStream fs = null;

            string fileName = filename;

            if (File.Exists(fileName) == false)
            {
                _container = default(T);
                return false;
            }

            try
            {

                // Create an instance of the XmlSerializer class of type Contact array 

                XmlSerializer x = new XmlSerializer(typeof(T));

                fs = new FileStream(fileName, FileMode.Open, FileAccess.Read);

                // Deserialize the content of the XML file to a ....
                // utilizing XMLReader
                XmlReader reader = new XmlTextReader(fs);
                T container = (T)x.Deserialize(reader);
                _container = container;
                fs.Close();

            }

            catch (Exception)
            {
                // Do nothing if the file does not exists
                if (fs != null) fs.Close();
                _container = default(T);
                return false;

            }

            if (fs != null) fs.Close();

            return true;

        }

    }
}

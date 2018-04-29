using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace Core.Models
{
    public static class Serializer
    {
        #region Serialization

        /// <summary>
        /// Serialize a Configuration to file
        /// </summary>
        /// <param name="configuration">Configuration to serialize</param>
        /// <returns>True if serialized correctly, False otherwise</returns>
        public static bool Serialize(object obj,string filePath)
        {
            IFormatter formatter = new BinaryFormatter();
            try
            {
                Stream stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
                formatter.Serialize(stream, obj);
                stream.Close();
            }
            catch
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Deserializes a configuration from file
        /// </summary>
        /// <returns>A configuration if found and deserialized correctly, null otherwise</returns>
        public static Configuration Deserialize(string filePath)
        {
            Configuration c = null;

            if (!File.Exists(filePath))
                return null;
            IFormatter formatter = new BinaryFormatter();
            try
            {
                Stream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                c = (Configuration)formatter.Deserialize(stream);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }

            return c;
        }

        #endregion
    }
}

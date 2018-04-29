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
        public static bool Serialize<T>(T obj,string filePath)
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
        public static T Deserialize<T>(string filePath)
        {
            T obj = default(T);

            if (!File.Exists(filePath))
                return default(T);
            IFormatter formatter = new BinaryFormatter();
            try
            {
                Stream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                obj = (T) formatter.Deserialize(stream);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }

            return obj;
        }

        #endregion
    }
}

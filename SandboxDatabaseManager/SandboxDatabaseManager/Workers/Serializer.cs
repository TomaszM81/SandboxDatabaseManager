/*******************************************************************************
 *******************************************************************************
            Author: Simon Bridge, May 2011 mailto:srbridge@gmail.com
 
 
        This code is provided under the Code Project Open Licence (CPOL)
          See http://www.codeproject.com/info/cpol10.aspx for details
  
 *******************************************************************************
 ******************************************************************************/

using System;
using System.IO;
using System.Text;
using System.Xml.Serialization;

namespace General.IO
{
    /// <summary>
    /// provides simple, strongly typed xml serialization methods.
    /// </summary>
    static class Serializer
    {
        /// <summary>
        /// returns the serializer for a specific type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        private static XmlSerializer getSerializer<T>()
        {
            return new XmlSerializer(typeof(T));
        }

        /// <summary>
        /// serializes data of type T to the specified filename.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="o"></param>
        /// <param name="fileName"></param>
        public static void Save<T>(T o, String fileName)
        {
            using (FileStream fs = File.Open(fileName, FileMode.Create))
            {
                Save<T>(o, fs);
            }
        }

        /// <summary>
        /// serialize data of type T to the specified stream.
        /// </summary>
        /// <typeparam name="T">
        /// the type of data to serialize.
        /// </typeparam>
        /// <param name="o">
        /// the object to serialize (must be of type t)
        /// </param>
        /// <param name="stream">
        /// the stream to write the serialized data to.
        /// </param>
        public static void Save<T>(T o, Stream stream)
        {
            getSerializer<T>().Serialize(stream, o);
        }

        /// <summary>
        /// get the xml bytes the object of type T is serialized to.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="o"></param>
        /// <returns></returns>
        public static byte[] Save<T>(T o)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                // write the data to the memory stream:
                Save<T>(o, ms);

                // return the back-buffer:
                return ms.GetBuffer();
            }
        }

        /// <summary>
        /// get the xml string the object is seriaized to, using the specified encoding.
        /// </summary>
        /// <typeparam name="T">
        /// the type of object to serialize.
        /// </typeparam>
        /// <param name="o">
        /// the object to serialize.
        /// </param>
        /// <returns></returns>
        public static String Save<T>(T o, Encoding encoding)
        {
            return encoding.GetString(Save<T>(o));
        }

        /// <summary>
        /// loads data of type T from the stream.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="o"></param>
        /// <param name="stream"></param>
        /// <returns></returns>
        public static T Load<T>(Stream stream)
        {
            return (T)getSerializer<T>().Deserialize(stream);
        }

        /// <summary>
        /// loads the data from the file.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="o"></param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public static T Load<T>(FileInfo file)
        {
            using (FileStream fs = file.Open(FileMode.Open))
            {
                return Load<T>(fs);
            }
        }

        /// <summary>
        /// deserialize an object of type T from the byte-array of XML data.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="data"></param>
        /// <returns></returns>
        public static T Load<T>(byte[] data)
        {
            using (MemoryStream ms = new MemoryStream(data))
            {
                return Load<T>(ms);
            }
        }

        /// <summary>
        /// deserialize an object of type T from the supplied XML data string.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="data"></param>
        /// <returns></returns>
        public static T Load<T>(String data)
        {
            return Load<T>(Encoding.ASCII.GetBytes(data));
        }

    }
}

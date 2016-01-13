/*******************************************************************************
 *******************************************************************************
            Author: Simon Bridge, May 2011 mailto:srbridge@gmail.com
 
 
        This code is provided under the Code Project Open Licence (CPOL)
          See http://www.codeproject.com/info/cpol10.aspx for details
  
 *******************************************************************************
 ******************************************************************************/

using System;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security;
using System.Security.Cryptography;
using General.IO;

namespace General.Security
{
    /// <summary>
    /// the license agreement for the software.
    /// embedded are the license terms (ie start and end dates) and a digital signature used to verify the 
    /// the license terms. this way, the consumer may be able to see what the license terms are, but if they attempt to change them
    /// (in order to extend thier license) then they will not be able to generate a matching signature.
    /// </summary>
    public class License
    {
        #region Properties

        /// <summary>
        /// the license terms. obscured.
        /// </summary>
        public string LicenseTerms { get; set; }

        /// <summary>
        /// the signature.
        /// </summary>
        public string Signature { get; set; }

        #endregion

        #region Methods

        /// <summary>
        /// saves the license to an xml file.
        /// </summary>
        /// <param name="fileName"></param>
        public void Save(String fileName)
        {
            Serializer.Save<License>(this, fileName);
        }

        /// <summary>
        /// saves the license to a stream as xml.
        /// </summary>
        /// <param name="stream"></param>
        public void Save(Stream stream)
        {
            Serializer.Save<License>(this, stream);
        }

        /// <summary>
        /// create a license object from a license file.
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public static License Load(String fileName)
        {
            // read the filename:
            return Serializer.Load<License>(new FileInfo(fileName));
        }

        /// <summary>
        /// load a license from stream xml data.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static License Load(Stream data)
        {
            // read the data stream:
            return Serializer.Load<License>(data);
        }

        #endregion
    }



    public class LicenseAuthorization
    {

        [Serializable]
        public class LicenseTerms
        {

            internal DateTime StartDate { get; set; }

            internal String UserName { get; set; }

            internal String ProductName { get; set; }

            internal String HardwareIdentifiers { get; set; }

            internal DateTime EndDate { get; set; }

            public DateTime ValidForBuildsTill { get; set; }

            internal byte Data { get; set; }

            internal String GetLicenseString()
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    // create a binary formatter:
                    BinaryFormatter bnfmt = new BinaryFormatter();

                    // serialize the data to the memory-steam;
                    bnfmt.Serialize(ms, this);

                    // return a base64 string representation of the binary data:
                    return Convert.ToBase64String(ms.GetBuffer());

                }
            }


            internal byte[] GetLicenseData()
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    // create a binary formatter:
                    BinaryFormatter bnfmt = new BinaryFormatter();

                    // serialize the data to the memory-steam;
                    bnfmt.Serialize(ms, this);

                    // return a base64 string representation of the binary data:
                    return ms.GetBuffer();

                }
            }


            internal static LicenseTerms FromString(String licenseTerms)
            {
                log4net.ILog Log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

                using (MemoryStream ms = new MemoryStream(Convert.FromBase64String(licenseTerms)))
                {
                    // create a binary formatter:
                    BinaryFormatter bnfmt = new BinaryFormatter();


                        object value = bnfmt.Deserialize(ms);

                        if (value is LicenseTerms)
                            return (LicenseTerms)value;
                        else
                            throw new ApplicationException("Invalid Type!");
                  
                }
            }

        }


    }
}

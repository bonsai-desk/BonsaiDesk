using System;
using UnityEngine.Networking;
using System.Security.Cryptography.X509Certificates;
using UnityEngine;

namespace NobleConnect.Internal
{
    #if !UNITY_5 && !UNITY_2017
    public class AmazonCertificateHandler : CertificateHandler
    {
        // Encoded RSAPublicKey
        private static readonly string PUB_KEY = "3082010A0282010100CCCC0263917F8002CD937BEDCD06F29" +
                                                 "919F82E724EE5012E2C02991AD7FA06603D927975B099FF23" +
                                                 "CA94A876ED8F9871051ED81A13D1486534BCF1B07277DC0DA" +
                                                 "26C79C03B58590C9B2724917C68E3A10368ABFC214C20D8DF" +
                                                 "E3BC8ACE0519C776E9F9EC93FADD6C6A5E1CDA1A24867D67F" +
                                                 "EC0AF2B0DB8DD1BBB7D020331B7C0ECF838B57C73E6FF6722" +
                                                 "F17A9DF74391CFC4914B44B77655B79223E1E550F715AD6F1" +
                                                 "4BF3E755FFF815526C162BB3F379FFB5272D3C0EA59A35D1E" +
                                                 "9E49B94872EB768250BA3DBBFFD042015BDD2B1DFD164B950" +
                                                 "6D7DA324B63348122CE9EB368DB2FF0E02AD6B7A453A28B11" +
                                                 "E67591FC2CC0B74E1C80688C535D32DC692C08ED0203010001";

        /// <summary>
        /// Validate the Certificate Against the Amazon public Cert
        /// </summary>
        /// <param name="certificateData">Certifcate to validate</param>
        /// <returns></returns>
        protected override bool ValidateCertificate(byte[] certificateData)
        {
            X509Certificate2 certificate = new X509Certificate2(certificateData);
            string pk = certificate.GetPublicKeyString();
            if (pk.ToLower().Equals(PUB_KEY.ToLower()))
            {
                return true;
            }

            return false;
        }
    }
    #endif
}
using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using AutoFixture;

namespace PdfLittleSignerSpecification;

public class X509CertificateTestingUtils
{
    public static X509Certificate2 GenerateX509Certificate2WithRsaKey(string commonName)
    {
        var rsa = RSA.Create();
        var subjectName = new X500DistinguishedName($"CN={commonName}");
        var certRequest = new CertificateRequest(subjectName, rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        X509Certificate2 cert = certRequest.CreateSelfSigned(DateTimeOffset.Now, DateTimeOffset.Now.AddYears(5));
        return cert;
    }

    public static X509Certificate2 GenerateX509Certificate2WithEcdsaKey(string commonName)
    {
        var ecdsa = ECDsa.Create();
        var subjectName = new X500DistinguishedName($"CN={commonName}");
        var certRequest = new CertificateRequest(subjectName, ecdsa, HashAlgorithmName.SHA256);
        X509Certificate2 cert = certRequest.CreateSelfSigned(DateTimeOffset.Now, DateTimeOffset.Now.AddYears(5));
        return cert;
    }
}
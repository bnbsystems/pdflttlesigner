
PDF Little Signer is a .NET6.0 library for self signing PDF document.  It uses iText7.

 

It's very easy to use:

```csharp
var cert = new X509Certificate2("cert_file.pfx", "cert_password",X509KeyStorageFlags.Exportable);

var pdfSigner = new PdpSigner("output.pdf");
var result = await pdfSigner.Sign("ReasonToSign", "Contact", "PhysicalLocation", true, stampImage, cert, fileToSign);
and you will get a signed pdf.
```

To generate certificate and generate .pfx file which contains both certificate andkey you can use OpenSSL tool. Install OpenSSL and then open command line or powershell and eneter following commands:
```
openssl req -x509 -newkey rsa:4096 -keyout pkey.key -out cert.cer -sha256 -days 365
openssl pkcs12 -export -out cert_file.pfx -inkey pkey.key -in cert.cer
```
You can also generate certificate from code using built in System library. Code below generates basic certificate with least amount of data needed for testing purposes:

```csharp
var rsa = RSA.Create();
var subjectName = new X500DistinguishedName("CN=test");
var certRequest = new CertificateRequest(subjectName, rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
X509Certificate2 cert = certRequest.CreateSelfSigned(DateTimeOffset.Now, DateTimeOffset.Now.AddYears(5));
```

Now available under NuGet. Just type:
```
PM> Install-Package PDFLittleSigner
```

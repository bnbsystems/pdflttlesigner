# PDF Little Signer

PDF Little Signer is a .NET6.0 library for self signing PDF document. It uses iText7.

It's very easy to use:

```csharp
var cert = new X509Certificate2("cert_file.pfx", "cert_password", X509KeyStorageFlags.Exportable);

var pdfSigner = new PdpSigner("output.pdf", null);
var result = await pdfSigner.Sign("ReasonToSign", "Contact", "PhysicalLocation", true, stampImage, cert, fileToSign);
```
and you will get a signed PDF.


To generate .pfx file which contains both certificate and key you can use OpenSSL tool. Build and install OpenSSL from GitHub source code following documentation instructions or download prebuilt binaries from thrid party binary distributors (some of them even provide self-install executables):

- OpenSSL GitHub: [https://github.com/openssl/openssl](https://github.com/openssl/openssl) 

- Wiki with links to third party distributors: [https://wiki.openssl.org/index.php/Binaries](https://wiki.openssl.org/index.php/Binaries) 

After installing OpenSSL you should edit PATH variable. To do this you need to:
1. Hit the Windows button on your keyboard or click it in the task bar, then search for "Environment Variables" and System Properties window will pop.
2. In advanced tab click Environment Variables button and screen will pop up showing User variables and System variables.
3. In the User variables section, select Path and click Edit.
4. Select empty field, click browse, navigate to OpenSSL bin folder and click OK. Path to OpenSSL binaries should look somewhat like this: "C:\Program Files\OpenSSL-Win64\bin".
5. Click OK to save the changes. Now you should be able to run openssl comands in command-line.

After installing OpenSSL run folowing commands in command-line to generate .pfx file.

```powershell
openssl req -x509 -newkey rsa:4096 -keyout pkey.key -out cert.cer -sha256 -days 365
openssl pkcs12 -export -out cert_file.pfx -inkey pkey.key -in cert.cer
```

You can also generate certificate from code using built-in System library. Code below generates basic certificate with least amount of data needed for testing purposes:

```csharp
var rsa = RSA.Create();
var subjectName = new X500DistinguishedName("CN=test");
var certRequest = new CertificateRequest(subjectName, rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
X509Certificate2 cert = certRequest.CreateSelfSigned(DateTimeOffset.Now, DateTimeOffset.Now.AddYears(5));
```

If you want to enable logging, then you can pass any logger which extends Microsoft.Extensions.Logging.ILogger interface to constructor. Example of injecting simple console logger using Microsoft.Extensions.Logging.ILoggerFactory below:

```csharp
using ILoggerFactory loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
var pdfSigner = new PdpSigner("output.pdf", loggerFactory.CreateLogger<PdpSigner>());
```

Now available under NuGet. Just type:
```powershell
PM> Install-Package PDFLittleSigner
```

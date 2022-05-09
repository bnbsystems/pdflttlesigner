
PDF Little Signer is a .NET6.0 library for self signing PDF document.  It uses iText7.

 

It's very easy to use:
```csharp
var cert = new X509Certificate2("cert_file.pfx", "cert_password",X509KeyStorageFlags.Exportable);

var pdfSigner = new PdpSigner("output.pdf");
var visible = true;

var result = await pdfSigner.Sign("ReasonToSign", "Contact", "PhysicalLocation", visible, stampImage, cert, fileToSign);
```
and you will get a signed pdf.

 

Now available under NuGet. Just type:
```
PM> Install-Package PDFLittleSigner
```


PDF Little Signer is a .NET3.5 library for self signing PDF document.  It uses iTextSharp.

 

It's very easy to use:
```csharp
MetaData metadata = new MetaData();
metadata.Author = "me";
PdpSigner signer = new PdpSigner("CertyficateName", "file.pdf", "file_signed.pdf", metadata);
bool signed = signer.Sign("reasonToSign","Contact", "PhysicalLocation", true, "image.jpg");
```
and you will get a signed pdf.

 

Now available under NuGet. Just type:
```
PM> Install-Package PDFLittleSigner
```

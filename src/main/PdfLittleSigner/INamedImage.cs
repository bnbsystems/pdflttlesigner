namespace PdfLittleSigner
{
    public interface INamedImage
    {
        string Name { get; }
        byte[] Data { get; }

    }
}

using UglyToad.PdfPig;
using WebApiCommon;

namespace PdfService
{
    public class PdfTextExtractor : IPdfTextExtractor
    {
        public IEnumerable<string> ExtractText(byte[] pdfData)
        {
            using (PdfDocument document = PdfDocument.Open(pdfData))
            {
                foreach (var page in document.GetPages())
                {
                    yield return page.Text;
                }
            }
        }
    }
}

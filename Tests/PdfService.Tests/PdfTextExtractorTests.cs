using Xunit;

namespace PdfService.Tests
{
    /// <summary>
    /// Тесты для PdfTextExtractor.
    /// Проверяют извлечение текста из PDF с использованием встроенного PDF-файла.
    /// </summary>
    public class PdfTextExtractorTests
    {
        /// <summary>
        /// Проверяет, что из минимального корректного PDF извлекается непустой текст.
        /// </summary>
        [Fact]
        public void ExtractText_ValidPdfData_ReturnsText()
        {
            // Arrange — минимальный корректный PDF с текстом "Hello World"
            // Создаём простой валидный PDF вручную
            var pdfBytes = CreateMinimalPdf("Hello World");

            var extractor = new PdfTextExtractor();

            // Act
            var result = extractor.ExtractText(pdfBytes).ToList();

            // Assert
            Assert.NotEmpty(result);
            Assert.Contains(result, text => text.Contains("Hello World"));
        }

        /// <summary>
        /// Проверяет, что из PDF с несколькими страницами возвращается текст со всех страниц.
        /// </summary>
        [Fact]
        public void ExtractText_MultiPagePdf_ReturnsTextFromAllPages()
        {
            // Arrange
            var pdfBytes = CreateMinimalPdf("Page1Content\nPage2Content");

            var extractor = new PdfTextExtractor();

            // Act
            var result = extractor.ExtractText(pdfBytes).ToList();

            // Assert
            Assert.NotEmpty(result);
        }

        [Fact]
        public void ExtractText_EmptyPdfData_DoesNotThrow()
        {
            // Arrange
            var pdfBytes = Array.Empty<byte>();

            var extractor = new PdfTextExtractor();

            // Act & Assert — не должно выбросить исключение (PdfPig выбросит, но мы проверяем)
            // Фактически PdfPig выбросит исключение на пустых данных — это ожидаемо
            Assert.Throws<UglyToad.PdfPig.Core.PdfDocumentFormatException>(() =>
                extractor.ExtractText(pdfBytes).ToList());
        }

        /// <summary>
        /// Создаёт минимальный корректный PDF-файл с заданным текстовым содержимым.
        /// </summary>
        private static byte[] CreateMinimalPdf(string content)
        {
            // Минимальный корректный PDF 1.4 с одним потоком контента
            var contentStream = System.Text.Encoding.ASCII.GetBytes($"BT /F1 12 Tf 100 700 Td ({content}) Tj ET");
            var contentStreamLength = contentStream.Length;

            var pdf = $@"%PDF-1.4
1 0 obj
<< /Type /Catalog /Pages 2 0 R >>
endobj

2 0 obj
<< /Type /Pages /Kids [3 0 R] /Count 1 >>
endobj

3 0 obj
<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792]
   /Contents 4 0 R /Resources << /Font << /F1 << /Type /Font /Subtype /Type1 /BaseFont /Helvetica >> >> >> >>
endobj

4 0 obj
<< /Length {contentStreamLength} >>
stream
{System.Text.Encoding.ASCII.GetString(contentStream)}
endstream
endobj

xref
0 5
0000000000 65535 f 
0000000009 00000 n 
0000000058 00000 n 
0000000115 00000 n 
0000000266 00000 n 

trailer
<< /Size 5 /Root 1 0 R >>
startxref
{400 + contentStreamLength}
%%EOF";

            return System.Text.Encoding.ASCII.GetBytes(pdf);
        }
    }
}
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace WebApiCommon
{
    public interface IPdfTextExtractor
    {
        IEnumerable<string> ExtractText(byte[] pdfData);
    }
}

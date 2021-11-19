using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace gekoke.JustParse.PDF {
    /// <summary>
    /// Used to parse a PDF - extract lines by page, or convert the lines to entries.
    /// Can parse either searchable PDFs which contain plaintext, or scanned PDFs (essentially images).
    /// If the PDF is searchable, the parsing is very quick. If the PDF is scanned, the parsing is quite slow.
    /// </summary>
    /// <typeparam name="E">The type of entries to parse from the given PDF</typeparam>
    public abstract class PDFEntryParser<E> {
        public string PathToPDF { get; set; }
        public bool IsScannedPDF { get; private init; }
        private readonly PDFParser pdfParser;

        /// <param name="isScanned">Whether the PDF is scanned and therefore contains no searchable text.</param>
        protected PDFEntryParser(
            string pathToPDF,
            string pathToTrainedModelDirectory,
            string language,
            Dictionary<string, string>? tesseractOptions,
            bool? isScanned = null
        ) {
            pdfParser = new(pathToTrainedModelDirectory, language, tesseractOptions);
            IsScannedPDF = (isScanned == null) ? PDFParser.IsScannedPDF(pathToPDF) : (bool)isScanned;
            PathToPDF = pathToPDF;
        }


        /// <summary>
        /// Parses the PDF and returns the list of <see cref="TypedEntry"/> contained in it.
        /// These contain the relevant information that will be entered into SAP later.
        /// </summary>
        /// <returns> A list of <see cref="TypedEntry"/> objects </returns>
        public async Task<List<E>> GetEntries() {
            return GetEntryLines(await pdfParser.GetLines(PathToPDF, isScannedPDF: IsScannedPDF))
                .Select(line => ConvertLineToEntry(line))
                .ToList();
        }

        /// <summary>
        /// Given a line representing an entry extracted from a PDF,
        /// convert it to an <see cref="Entry">. 
        ///
        /// This method will determine which lines in the PDF will
        /// be interpreted as a <see cref="Entry"/>. Each subclass of this class
        /// will have to provide its own implementation.
        /// </summary>
        /// <param name="line"> The given line extracted from the PDF. </param>
        /// <returns> The line parsed into a <see cref="Entry">. </returns>
        protected abstract E ConvertLineToEntry(string line);

        /// <returns> All lines in the PDF that can be interpreted as an <see cref="Entry">. </returns>
        protected abstract List<string> GetEntryLines(List<string> lines);
    }
}

using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace gekoke.JustParse.PDF {
    public class PDFParser {
        private readonly string trainedModelDirectory;

        public PDFParser(string trainedModelDirectory = @"./tessdata") {
            this.trainedModelDirectory = trainedModelDirectory;
        }

        public async Task<string> GetText(string pathToPDF, bool? isScannedPDF = null) {
            return await Task.Run(() => {
                if (isScannedPDF == null) isScannedPDF = IsScannedPDF(pathToPDF);

                SortedDictionary<int, string> textByPage;
                if ((bool)isScannedPDF) textByPage = ScannedPDFToText.GetTextByPage(pathToPDF, trainedModelDirectory);
                else textByPage = ReadTextFromSearchablePDF(pathToPDF);

                return string.Join('\n', textByPage.Values);
            });
        }

        public async Task<SortedDictionary<int, string>> GetTextByPage(string pathToPDF, bool? isScannedPDF = null) {
            return await Task.Run(() => {
                if (isScannedPDF == null) isScannedPDF = IsScannedPDF(pathToPDF);

                SortedDictionary<int, string> linesByPage;
                if ((bool)isScannedPDF) linesByPage = ScannedPDFToText.GetTextByPage(pathToPDF, trainedModelDirectory);
                else linesByPage = ReadTextFromSearchablePDF(pathToPDF);

                return linesByPage;
            });
        }

        ///<summary>
        /// Parses the PDF for text and returns list of lines.
        /// Since the PDF format doesn't actually contain any lines (text only has geometric position),
        /// this parsing is based on heuristics and may fail.
        ///<summary>
        ///<returns> A list of "lines" contained in this PDF. </returns>
        public async Task<List<string>> GetLines(string pathToPDF, bool? isScannedPDF = null) {
            return await Task.Run(async () => {
                if (isScannedPDF == null) isScannedPDF = IsScannedPDF(pathToPDF);
                return (await GetLinesByPage(pathToPDF, isScannedPDF)).Values.SelectMany(pageLines => pageLines).ToList();
            });
        }

        public async Task<SortedDictionary<int, List<string>>> GetLinesByPage(string pathToPDF, bool? isScannedPDF = null) {
            return await Task.Run(() => {
                if (isScannedPDF == null) isScannedPDF = IsScannedPDF(pathToPDF);

                SortedDictionary<int, List<string>> linesByPage;
                if ((bool)isScannedPDF) linesByPage = ScannedPDFToText.GetLinesByPage(pathToPDF, trainedModelDirectory: trainedModelDirectory);
                else linesByPage = ReadLinesFromSearchablePDF(pathToPDF);

                return linesByPage;
            });
        }

        public static bool IsScannedPDF(string pathToPDF) {
            var pages = ReadLinesFromSearchablePDF(pathToPDF);
            return pages.Count <= 2 && pages.All(page => page.Value.All(line => line.Trim() == string.Empty));
        }

        internal static SortedDictionary<int, List<string>> ReadLinesFromSearchablePDF(string pathToPDF) {
            var pdfDocument = new PdfDocument(new PdfReader(new FileStream(pathToPDF, FileMode.Open, FileAccess.Read)));
            var linesByPage = new SortedDictionary<int, List<string>>();

            for (int i = 1; i <= pdfDocument.GetNumberOfPages(); i++) {
                LocationTextExtractionStrategy strategy = new();
                PdfCanvasProcessor parser = new(strategy);

                // Library throws exceptions we don't care about but still parses page correctly so *shrug*
                try { parser.ProcessPageContent(pdfDocument.GetPage(i)); } catch { }
                linesByPage.Add(i, strategy.GetResultantText().Split("\n").ToList());
            }

            return linesByPage;
        }

        internal static SortedDictionary<int, string> ReadTextFromSearchablePDF(string pathToPDF) {
            SortedDictionary<int, string> output = new();
            foreach (var pair in ReadLinesFromSearchablePDF(pathToPDF)) output[pair.Key] = string.Join("\n", pair.Value);
            return output;
        }
    }
}

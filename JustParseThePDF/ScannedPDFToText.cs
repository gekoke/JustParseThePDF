using gekoke.JustParse.Image;
using gekoke.JustParse.OCR;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace gekoke.JustParse.PDF {
    internal class ScannedPDFToText {
        public static SortedDictionary<int, string> GetTextByPage(
            string pdfPath,
            string trainedModelDirectory,
            string language,
            Dictionary<string, string>? tesseractOptions = null
        ) {
            SortedDictionary<int, string> textByPage = new();

            string tempDir = Path.GetRandomFileName();
            Directory.CreateDirectory(tempDir);

            var imagePaths = PDFToImage.Convert(pdfPath, tempDir);
            for (int i = 0; i < imagePaths.Count; i++)
                textByPage[i + 1] = ImageTextExtractor.GetImageText(imagePaths[i], trainedModelDirectory, language, tesseractOptions);

            Directory.Delete(tempDir, recursive: true);

            return textByPage;
        }

        /// <param name="discardEmptyLines">Discards lines where performing <see cref="string.Trim"/> would result in a <see cref="string.Empty"/></param>
        public static SortedDictionary<int, List<string>> GetLinesByPage(
            string pdfPath,
            string trainedModelDirectory,
            string language,
            Dictionary<string, string>? tesseractOptions = null
        ) {
            var textByPage = GetTextByPage(pdfPath, trainedModelDirectory, language, tesseractOptions);
            SortedDictionary<int, List<string>> linesByPage = new();

            foreach (var pair in textByPage)
                linesByPage[pair.Key] = pair.Value.Split("\n").ToList();

            return linesByPage;
        }
    }
}

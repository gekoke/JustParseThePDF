using System.Collections.Generic;
using Tesseract;

namespace gekoke.JustParse.OCR {
    public class ImageTextExtractor {
        /// <summary>
        /// Extract the text from the given image using optical character recognition.
        /// </summary>
        /// <param name="sourceFilePath">The image to extract text from</param>
        /// <param name="dpi">The resolution to use when scanning the image for text (higher is more accurate but slower - default is 500)</param>
        /// <returns>The text as parsed from the input image</returns>
        public static string GetImageText(
            string sourceFilePath,
            string trainedModelDirectory,
            string language,
            Dictionary<string, string>? tesseractOptions = null
        ) {
            using var engine = new TesseractEngine(trainedModelDirectory, language);
            if (tesseractOptions != null)
                foreach (var pair in tesseractOptions) engine.SetVariable(pair.Key, pair.Value);

            using var img = Pix.LoadFromFile(sourceFilePath);
            using var page = engine.Process(img);
            return page.GetText();
        }
    }
}

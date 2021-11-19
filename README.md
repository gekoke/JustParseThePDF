

## 📄 Overview
This PDF parsing library enables you to read the text from either a PDF which already contains text, or a scanned PDF (no text contained in file) using [Tesseract OCR](https://github.com/tesseract-ocr/tesseract).
If the text is already present in the PDF file, the read is near-instantaneous. The OCR method takes considerably longer.

## Credit
Thanks to StackOverflow user HamedH. The PDF to image conversion portion of this library is a modified version of their code snippet
from the thread https://stackoverflow.com/questions/23905169/how-to-convert-pdf-files-to-images.

## Install
[![NuGet version (JustParseThePDF)](https://img.shields.io/nuget/v/JustParseThePDF)](https://www.nuget.org/packages/JustParseThePDF/)
```shell
dotnet add package JustParseThePDF
```

## Requirements
You must supply a directory which contains a trained Tesseract model with the filename "eng.traineddata" to the parser. (You can get a pretrained model file from [here](https://github.com/tesseract-ocr/tessdata/blob/main/eng.traineddata). Make sure to copy the file to the build output directory! In Visual Studio, you can do this by performing: Select file in Solution Explorer -> Properties window -> Copy to Output Directory -> Always)

## Limitations
- This library currently only supports Windows, as the PDF to image conversion relies on System.Drawing.Common. It shouldn't be too difficult to change this to be a cross-platform implementation, though. More information on this can be found at https://docs.microsoft.com/en-us/dotnet/core/compatibility/core-libraries/6.0/system-drawing-common-windows-only.

- Since most of the methods in the library are static, numerous temporary objects are constructed which may reduce the performance when issuing consecutive method calls. This isn't difficult to fix, either.

## Examples

### Parsing the contents of a PDF

#### Check whether a PDF is scanned (is solely an image) or is searchable (contains text)
```csharp
bool isScanned = PDFParser.IsScannedPDF("pathto/something.pdf");
```

#### Creating a parser
```csharp
using var engine = new TesseractEngine("path/to/trained/model", "eng");
var parser = new PDFParser(engine);
```

#### Parsing the text from a PDF by page.
```csharp
// You may provide the optional argument if you know for sure whether the PDF is scanned in advance or not.
// Providing the argument is slightly faster and more reliable, though an attempt is made to detect whether the file is a scanned PDF.
// From my usage, it seems that the detection is solid enough.
SortedDictionary<int, string> pages = await parser.GetTextByPage("pathto/something.pdf", isScannedPDF: true);

// You can also specify a specific segmentation mode to use - the default is automatic detection.
SortedDictionary<int, List<string>> pages = await parser.GetLinesByPage("pathto/something.pdf", pageSegmentationMode: PageSegMode.SingleBlock);
```

#### Parsing all the text from a PDF.
```csharp
// Watch out! Transitions from one page to another are not delimited by newline characters (or any character for that matter).
string text = await parser.GetText("pathto/something.pdf");
List<string> lines = await parser.GetLines("pathto/something.pdf");
```

### Parsing the contents of a PDF for entries of a certain format
Use this if you have a neat mapping A -> B, where A is a line in the PDF which meets certain criteria, and B is some data model.

Let's say you have a PDF which contains the following lines, and you want to extract the product information:

```txt
Lorem ipsum brukes som fylltekst i grafisk design og sideombrekking for å demonstr
før den endelige teksten er klargjort. Denne bruken betegnes som greeking
ID | Product           | Quantity | Amount
0  | Inflatable Amogus | 2        | 132.32
1  | Cheese            | 3        | 200.12
Lorem ipsum brukes som fylltekst i grafisk design og sideombrekking for å demonst
før den endelige teksten er klargjort. Denne bruken betegnes som greeking. Lipsum
```

You could define the parser like so:

```csharp
namespace example {
    public record ProductEntry(int Id, string Product, decimal Amount) { }

    public class ProductEntryParser : PDFEntryParser<ProductEntry> {
        public ProductEntryParser(string pathToPDF, TesseractEngine engine, bool? isScanned = null) : base(pathToPDF, engine, isScanned) { }

        // Define how to convert a line that is assumed to be an entry into the desired data type.
        protected override ProductEntry ConvertLineToEntry(string line) {
            var values = line
                .Split('|')                                // Get values delimited by pipe characters. 
                .Select(value => value.Trim()).ToArray();  // Remove leading and trailing whitespace from values.

            return new ProductEntry(
                Convert.ToInt32(values[0]),  
                values[1],
                // Conversion to decimal depends on the culture information of the current thread by default.
                // Make sure to specify this explicitly to avoid any formatting errors.
                // (that means, you have to specify whether you expect 123132,32 or 123.312,32 or 123,321.32 or ...)
                Convert.ToDecimal(values[2])
            );
        }

        // Choose which lines should count as entries.
        protected override List<string> GetEntryLines(List<string> lines) {
            // Some fairly safe qualifier that should almost always identify the correct lines:
            return lines.Where(line =>
                line != string.Empty
                && line.Contains("|")
                // Be careful! char.IsDigit doesn't return true only for ASCII chars 0-9, but any Unicode codepoint that might be considered a digit.
                && char.IsDigit(line[0])
            ).ToList();
        }
    }
}
```

And then read the entries like so:

```csharp
var engine = new TesseractEngine("path/to/trained/model", "eng");
var parser = new ProductEntryParser("pathto/mypdf.pdf");

var entries = await parser.GetEntries();
if (entries[0].Product != "Inflatable Amogus") throw new Exception("This should ideally never happen! :)");
```

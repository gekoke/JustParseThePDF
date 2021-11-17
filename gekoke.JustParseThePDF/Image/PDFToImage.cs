using PDFiumCore;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace gekoke.JustParse.Image {
    public class PDFToImage {
        /// <summary>
        /// Convert the pages of the input PDF into images.
        /// </summary>
        /// <param name="inputPDFPath">The PDF to convert</param>
        /// <param name="imageOutputDirectory">The directory to save the output images to</param>
        /// <param name="fileNameWithoutExtension">The filename to save the images as</param>
        /// <param name="imageFormat">The format to save the images in</param>
        /// <param name="scale">The factor to upscale/downscale the image by (default 1.0)</param>
        /// <returns>The paths to the images that were saved</returns>
        public static List<string> Convert(
            string inputPDFPath, string imageOutputDirectory = "",
            string fileNameWithoutExtension = "output_image", ImageFormat? imageFormat = null,
            float scale = 1f
        ) {
            List<string> savedImages = new();
            if (imageFormat == null) imageFormat = ImageFormat.Jpeg;
            fpdfview.FPDF_InitLibrary();

            int pageIndex = 0;
            while (true) {
                var document = fpdfview.FPDF_LoadDocument(inputPDFPath, null);
                var page = fpdfview.FPDF_LoadPage(document, pageIndex);
                if (page == null) break;  // No more pages
                var size = new FS_SIZEF_();

                fpdfview.FPDF_GetPageSizeByIndexF(document, 0, size);

                var width = (int)Math.Round(size.Width * scale);
                var height = (int)Math.Round(size.Height * scale);
                var bitmap = fpdfview.FPDFBitmapCreateEx(width, height, 4, IntPtr.Zero, 0);

                fpdfview.FPDFBitmapFillRect(bitmap, 0, 0, width, height, (uint)Color.White.ToArgb());

                using var matrix = ConstructMatrix(scale);
                using var clipping = ConstructClipping(width, height);
                fpdfview.FPDF_RenderPageBitmapWithMatrix(bitmap, page, matrix, clipping, (int)RenderFlags.RenderAnnotations);


                string fileName = Path.Join(imageOutputDirectory, fileNameWithoutExtension + $"_{pageIndex++}" + "." + imageFormat.ToString().ToLower());
                ConstructSystemBitmap(width, height, bitmap).Save(fileName, imageFormat);
                savedImages.Add(fileName);
            }

            return savedImages;
        }

        private static FS_MATRIX_ ConstructMatrix(float scale) {
            return new FS_MATRIX_ {
                A = scale,
                B = 0,
                C = 0,
                D = scale,
                E = 0,
                F = 0,
            };
        }

        private static FS_RECTF_ ConstructClipping(int width, int height) {
            return new FS_RECTF_ {
                Left = 0,
                Right = width,
                Bottom = 0,
                Top = height
            };
        }

        private static Bitmap ConstructSystemBitmap(int width, int height, FpdfBitmapT bitmap) {
            return new Bitmap(
                width,
                height,
                fpdfview.FPDFBitmapGetStride(bitmap),
                PixelFormat.Format32bppArgb,
                fpdfview.FPDFBitmapGetBuffer(bitmap)
            );
        }
    }
}

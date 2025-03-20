using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using iText.Kernel.Utils;
using iText.IO.Image;
using iText.Kernel.Geom;
using iText.Kernel.Pdf.Xobject;
using iText.Kernel.Pdf.Colorspace;
using iText.Kernel.Pdf.Extgstate;


namespace GazApps
{
    public class PDFToHTMLConverter
    {
        public static void Main(string[] args)
        {
            if (args.Length != 1)
            {
                Console.WriteLine("Uso: PDFToHTMLConverter <pasta>");
                return;
            }

            string folderPath = args[0];
            if (!Directory.Exists(folderPath))
            {
                Console.WriteLine("Caminho inválido!");
                return;
            }

            var files = Directory.GetFiles(folderPath, "*.pdf").OrderBy(f => f).ToList();

            if (files.Count == 0)
            {
                Console.WriteLine("Nenhum arquivo PDF encontrado.");
                return;
            }

            Console.WriteLine("Gerando arquivos HTML..... ");
            Directory.GetFiles(folderPath, "*.pdf")
                .ToList()
                .ForEach(ConvertToHtml);

            Console.WriteLine("Gerando combinado..... ");
            MergePdfFiles(folderPath);

            Console.WriteLine("Concluído");
        }

        private static void ConvertToHtml(string pdfPath)
        {
            try
            {
                // Extrair texto
                string text = ExtractTextFromPdf(pdfPath);

                // Renderizar imagens (captura as imagens contidas no PDF)
                List<string> base64Images = ExtractImagesFromPdf(pdfPath);

                // Construir HTML
                StringBuilder html = new StringBuilder("<html><body>");
                html.Append("<h2>").Append(System.IO.Path.GetFileName(pdfPath)).Append("</h2>");
                html.Append("<p>").Append(text.Replace("\n", "<br>")).Append("</p>");

                foreach (string base64Image in base64Images)
                {
                    html.Append("<img src='data:image/png;base64,").Append(base64Image).Append("'/><br>");
                }

                html.Append("</body></html>");

                // Salvar arquivo HTML
                string htmlPath = pdfPath.Replace(".pdf", ".html");
                File.WriteAllText(htmlPath, html.ToString());
                Console.WriteLine("Convertido: " + htmlPath);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Erro ao converter {pdfPath}: {ex.Message}");
            }
        }

        private static string ExtractTextFromPdf(string pdfPath)
        {
            StringBuilder text = new StringBuilder();

            using (PdfReader reader = new PdfReader(pdfPath))
            using (PdfDocument pdfDoc = new PdfDocument(reader))
            {
                for (int i = 1; i <= pdfDoc.GetNumberOfPages(); i++)
                {
                    ITextExtractionStrategy strategy = new SimpleTextExtractionStrategy();
                    string pageText = PdfTextExtractor.GetTextFromPage(pdfDoc.GetPage(i), strategy);
                    text.Append(pageText).Append("\n");
                }
            }
            return text.ToString();
        }

        private static List<string> ExtractImagesFromPdf(string pdfPath)
        {
            List<string> base64Images = new List<string>();

            using (PdfReader reader = new PdfReader(pdfPath))
            using (PdfDocument pdfDoc = new PdfDocument(reader))
            {
                for (int i = 1; i <= pdfDoc.GetNumberOfPages(); i++)
                {
                    PdfPage page = pdfDoc.GetPage(i);
                    var resources = page.GetResources();
                    var xObjects = resources.GetResource(PdfName.XObject);

                    if (xObjects != null)
                    {
                        foreach (var entry in xObjects.EntrySet())
                        {
                            PdfStream stream = entry.Value as PdfStream;
                            if (stream == null || !PdfName.Image.Equals(stream.GetAsName(PdfName.Subtype)))
                                continue;

                            byte[] imgBytes = stream.GetBytes();
                            string base64Image = Convert.ToBase64String(imgBytes);
                            base64Images.Add(base64Image);
                        }
                    }
                }
            }

            return base64Images;
        }

        public static void MergePdfFiles(string pdfPath)
        {
            var files = Directory.GetFiles(pdfPath, "*.pdf").OrderBy(f => f).ToList();

            if (files.Count == 0)
            {
                Console.WriteLine("Nenhum arquivo PDF encontrado.");
                return;
            }

            string outputFilePath = System.IO.Path.Combine(pdfPath, "Combinados.pdf");

            if (File.Exists(outputFilePath))
            {
                File.Delete(outputFilePath);
            }

            using (var pdfWriter = new PdfWriter(outputFilePath))
            using (var pdfDocument = new PdfDocument(pdfWriter))
            {
                var merger = new PdfMerger(pdfDocument);

                foreach (var file in files)
                {
                    using (var pdfReader = new PdfReader(file))
                    using (var sourcePdfDocument = new PdfDocument(pdfReader))
                    {
                        merger.Merge(sourcePdfDocument, 1, sourcePdfDocument.GetNumberOfPages());
                    }
                }
            }

            Console.WriteLine($"PDFs mesclados com sucesso em: {outputFilePath}");
        }
    }
}

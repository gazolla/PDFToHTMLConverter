using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.parser;
using PdfiumViewer;
using log4net;
using log4net.Config;

[assembly: XmlConfigurator(Watch = true)]

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

            Directory.GetFiles(folderPath, "*.pdf")
                .ToList()
                .ForEach(ConvertToHtml);

            Console.WriteLine("concluido");
        }

        private static void ConvertToHtml(string pdfPath)
        {
            try
            {
                // Extrair texto
                string text = ExtractTextFromPdf(pdfPath);

                // Renderizar imagens
                List<string> base64Images = RenderPagesToBase64(pdfPath);

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
            {
                for (int i = 1; i <= reader.NumberOfPages; i++)
                {
                    string pageText = PdfTextExtractor.GetTextFromPage(reader, i);
                    text.Append(pageText);
                }
            }
            return text.ToString();
        }

        private static List<string> RenderPagesToBase64(string pdfPath)
        {
            List<string> base64Images = new List<string>();

            try
            {
                // Usamos PdfiumViewer para renderizar as páginas PDF como imagens
                using (var document = PdfiumViewer.PdfDocument.Load(pdfPath))
                {
                    // Para cada página no documento
                    for (int i = 0; i < document.PageCount; i++)
                    {
                        // Definimos a resolução da imagem (dpi)
                        float dpi = 150.0f;

                        // Renderizamos a página como um Bitmap
                        using (var image = document.Render(i, dpi, dpi, PdfiumViewer.PdfRenderFlags.Annotations))
                        {
                            // Convertemos o bitmap para string base64
                            string base64 = EncodeToBase64((Bitmap)image);
                            base64Images.Add(base64);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Erro ao renderizar páginas: {ex.Message}");
                // Se falhar a renderização, ao menos retornamos uma lista vazia
                // para não quebrar a geração do HTML
            }

            return base64Images;
        }

        private static string EncodeToBase64(Bitmap image)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                image.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                return Convert.ToBase64String(ms.ToArray());
            }
        }
    }
}
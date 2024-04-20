using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;
using System.Net.Http.Headers;
using System.Text.Json;

namespace GitHubRepositoryAnalyzerApp
{
    class Program
    {
        static async Task Main(string[] args)
        {
            string repositoryOwner = "tpjtiago";
            string repositoryName = "GitHubRepositoryAnalyzerApp";
            string accessToken = "";

            string apiUrl = $"https://api.github.com/repos/{repositoryOwner}/{repositoryName}/commits";

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("GitHubRepositoryAnalyzer", "1.0"));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Token", accessToken);

                HttpResponseMessage response = await client.GetAsync(apiUrl);

                if (response.IsSuccessStatusCode)
                {
                    string responseBody = await response.Content.ReadAsStringAsync();

                    // Create PDF with commit data
                    CreatePdfWithCommitData(responseBody);
                }
                else
                {
                    Console.WriteLine($"Falha na solicitação: {response.StatusCode}");
                }
            }
        }

        static void CreatePdfWithCommitData(string responseBody)
        {
            // Cria um novo documento PDF
            PdfDocument document = new PdfDocument();

            // Adiciona uma página ao documento
            PdfPage page = document.AddPage();
            XGraphics gfx = XGraphics.FromPdfPage(page);

            // Define a fonte e tamanho do texto
            XFont font = new XFont("Arial", 12, XFontStyle.Regular);

            // Deserialize o JSON da resposta
            using (JsonDocument doc = JsonDocument.Parse(responseBody))
            {
                var commits = doc.RootElement.EnumerateArray();

                int totalCommits = 0;
                int copilotCommits = 0;

                foreach (var commit in commits)
                {
                    totalCommits++;
                    string commitMessage = commit.GetProperty("commit").GetProperty("message").GetString();
                    if (commitMessage.Contains("IA-(Projeto-X)", StringComparison.OrdinalIgnoreCase))
                    {
                        copilotCommits++;
                    }
                }

                double copilotPercentage = totalCommits > 0 ? (double)copilotCommits / totalCommits * 100 : 0;

                // Escreve os dados no documento PDF
                gfx.DrawString($"Total de commits: {totalCommits}", font, XBrushes.Black, new XRect(30, 30, page.Width.Point, page.Height.Point), XStringFormats.TopLeft);
                gfx.DrawString($"Commits com 'IA-(Projeto-X)': {copilotCommits}", font, XBrushes.Black, new XRect(30, 50, page.Width.Point, page.Height.Point), XStringFormats.TopLeft);
                gfx.DrawString($"Porcentagem de uso de IA: {copilotPercentage}%", font, XBrushes.Black, new XRect(30, 70, page.Width.Point, page.Height.Point), XStringFormats.TopLeft);
            }

            // Salva o documento PDF
            string pdfPath = @"C:\PDF-Git\Arquivo.pdf";
            document.Save(pdfPath);

            Console.WriteLine($"PDF criado com sucesso em: {pdfPath}");
        }
    }
}

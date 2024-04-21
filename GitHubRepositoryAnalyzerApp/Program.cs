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

                    // Create PDF with commit data and pie chart
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

            // Desenha o cabeçalho
            gfx.DrawRectangle(XBrushes.LightBlue, 0, 0, page.Width, 100);
            gfx.DrawString("Relatório de Utilização de Inteligência Artificial", new XFont("Arial", 20, XFontStyle.Bold), XBrushes.White, new XRect(0, 30, page.Width, 50), XStringFormats.Center);

            string dataInicial = "01/04/2024";
            string dataFinal = "20/04/2024";
            XSize size = gfx.MeasureString($"Período: {dataInicial} a {dataFinal}", font);
            double centerX = page.Width / 2;
            double textY = 100 + (size.Height / 2); // Posiciona o texto no centro abaixo do cabeçalho
            gfx.DrawString($"Período: {dataInicial} a {dataFinal}", font, XBrushes.Black, new XRect(0, textY, page.Width, size.Height), XStringFormats.Center);

            // Deserialize o JSON da resposta
            using (JsonDocument doc = JsonDocument.Parse(responseBody))
            {
                var commits = doc.RootElement.EnumerateArray();

                int totalCommits = 0;
                int copilotCommits = 0;
                Dictionary<string, int> authorCommitCounts = new Dictionary<string, int>();
                foreach (var commit in commits)
                {
                    totalCommits++;
                    string commitMessage = commit.GetProperty("commit").GetProperty("message").GetString();
                    string authorName = commit.GetProperty("author").GetProperty("login").GetString();
                    if (!authorCommitCounts.ContainsKey(authorName))
                    {
                        authorCommitCounts[authorName] = 0;
                    }

                    authorCommitCounts[authorName]++;

                    if (commitMessage.Contains("IA-(Projeto-X)", StringComparison.OrdinalIgnoreCase))
                    {
                        copilotCommits++;
                    }
                    //gfx.DrawString($"Mensagem: {commitMessage}", font, XBrushes.Black, new XRect(30, 190 + (totalCommits * 20), page.Width.Point, page.Height.Point), XStringFormats.TopLeft);

                }

                double copilotPercentage = totalCommits > 0 ? (double)copilotCommits / totalCommits * 100 : 0;

                // Escreve os dados no documento PDF
                gfx.DrawString($"Total de commits: {totalCommits}", font, XBrushes.Black, new XRect(30, 130, page.Width.Point, page.Height.Point), XStringFormats.TopLeft);
                gfx.DrawString($"Commits com 'IA-(Projeto-X)': {copilotCommits}", font, XBrushes.Black, new XRect(30, 150, page.Width.Point, page.Height.Point), XStringFormats.TopLeft);
                gfx.DrawString($"Porcentagem de uso de IA: {copilotPercentage.ToString("F2")}%", font, XBrushes.Black, new XRect(30, 170, page.Width.Point, page.Height.Point), XStringFormats.TopLeft);
                double chartX = page.Width - 150;
                double chartY = textY + 120;

                // Cria o gráfico de pizza
                double[] data = { copilotPercentage, 100 - copilotPercentage };
                string[] labels = { "IA-(Projeto-X)", "Tradicional" };
                DrawPieChart(gfx, data, labels, chartX, chartY, 100);

                // Desenha o gráfico de barras horizontais
                double barChartStartX = 30;
                double barChartStartY = 300;
                double barWidth = 200;
                double barHeight = 20;

                DrawBarChart(gfx, authorCommitCounts, barChartStartX, barChartStartY, barWidth, barHeight, copilotCommits);
            }

            // Salva o documento PDF
            string pdfPath = @"C:\PDF-Git\Arquivo1.pdf";
            document.Save(pdfPath);

            Console.WriteLine($"PDF criado com sucesso em: {pdfPath}");
        }




        static void DrawPieChart(XGraphics gfx, double[] data, string[] labels, double centerX, double centerY, double radius)
        {
            double total = data.Sum();
            double startAngle = 0;

            // Cores para as fatias do gráfico
            XColor[] sliceColors = { XColors.BlueViolet, XColors.MediumSeaGreen, XColors.Blue, XColors.Yellow, XColors.Orange, XColors.Purple };

            for (int i = 0; i < data.Length; i++)
            {
                double sweepAngle = 360 * (data[i] / total);

                // Seleciona uma cor para a fatia
                XBrush brush = new XSolidBrush(sliceColors[i % sliceColors.Length]);

                gfx.DrawPie(brush, (float)(centerX - radius), (float)(centerY - radius), (float)(2 * radius), (float)(2 * radius), (float)startAngle, (float)sweepAngle);

                double labelX = centerX + (radius / 2) * Math.Cos(Math.PI * (startAngle + sweepAngle / 2) / 180);
                double labelY = centerY + (radius / 2) * Math.Sin(Math.PI * (startAngle + sweepAngle / 2) / 180);
                gfx.DrawString($"{labels[i]} ({data[i]:F2}%)", new XFont("Arial", 10), XBrushes.Black, (float)labelX, (float)labelY, XStringFormats.Center);

                startAngle += sweepAngle;
            }
        }
        static void DrawBarChart(XGraphics gfx, Dictionary<string, int> commitCounts, double startX, double startY, double barWidth, double barHeight, int copilotCommits)
        {
            int maxCount = commitCounts.Values.Max();
            double scaleFactor = barWidth / maxCount;

            int i = 0;
            foreach (var pair in commitCounts)
            {
                double barLength = pair.Value * scaleFactor;
                double barX = startX;
                double barY = startY + i * (barHeight + 10);

                // Desenha a barra
                gfx.DrawRectangle(XBrushes.LightBlue, barX, barY, barLength, barHeight);

                // Adiciona o texto do autor e contagem de commits acima da barra
                gfx.DrawString($"I.A", new XFont("Arial", 10), XBrushes.Black, barX + barLength + 10, barY, XStringFormats.TopLeft);

                // Calcula a posição do texto no meio da barra
                double textX = barX + (barLength / 2);
                double textY = barY + (barHeight / 2);

                // Adiciona o texto no meio da barra
                gfx.DrawString($"{pair.Key}: {copilotCommits} ", new XFont("Arial", 10), XBrushes.Black, textX, textY, XStringFormats.Center);
                i++;
            }
        }

    }
}

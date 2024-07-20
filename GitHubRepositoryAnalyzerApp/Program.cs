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
            //TagsGitConfiguration.ConfigureGitHooks();

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
            // Define a altura do cabeçalho
            double headerHeight = 100;

            // Define as margens do texto
            double titleMarginTop = -40;
            double subtitleMarginTop = 60;

            // Desenha o cabeçalho com nova cor de fundo
            gfx.DrawRectangle(XBrushes.DarkSlateBlue, 0, 0, page.Width, headerHeight);

            // Adiciona uma linha abaixo do cabeçalho com uma cor que contrasta
            gfx.DrawLine(new XPen(XColors.WhiteSmoke, 2), 0, headerHeight, page.Width, headerHeight);

            // Adiciona o título com uma nova cor
            var titleFont = new XFont("Arial", 24, XFontStyle.Bold);
            var subtitleFont = new XFont("Arial", 14, XFontStyle.Italic);
            var titleBrush = XBrushes.LightCyan;
            var subtitleBrush = XBrushes.LightSteelBlue;

            // Desenha o título
            gfx.DrawString("Relatório de Utilização de Inteligência Artificial",
                titleFont, titleBrush,
                new XRect(0, titleMarginTop, page.Width, headerHeight - titleMarginTop),
                XStringFormats.Center);

            // Desenha um subtítulo opcional
            gfx.DrawString("Análise detalhada dos dados e insights",
                subtitleFont, XBrushes.White,
                new XRect(0, 30, page.Width, 50),
                XStringFormats.Center);

            string dataInicial = "01/04/2024";
            string dataFinal = "20/04/2024";
            XSize size = gfx.MeasureString($"Período: {dataInicial} a {dataFinal}", font);
            double centerX = page.Width / 2;
            double textY = 100 + (size.Height / 2); // Posiciona o texto no centro abaixo do cabeçalho

            gfx.DrawString($"Período: {dataInicial} a {dataFinal}", font, XBrushes.LightSteelBlue, new XRect(0, 75, page.Width, size.Height), XStringFormats.Center);

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
                gfx.DrawString($"Commits com Tag 'IA': {copilotCommits}", font, XBrushes.Black, new XRect(30, 150, page.Width.Point, page.Height.Point), XStringFormats.TopLeft);
                gfx.DrawString($"Porcentagem de uso de IA: {copilotPercentage.ToString("F2")}%", font, XBrushes.Black, new XRect(30, 170, page.Width.Point, page.Height.Point), XStringFormats.TopLeft);
                double chartX = page.Width - 150;
                double chartY = textY + 120;

                // Cria o gráfico de pizza
                double[] data = { copilotPercentage, 100 - copilotPercentage };
                string[] labels = { "IA-GiftCard", "Tradicional" };
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
            if (gfx == null || data == null || labels == null || data.Length != labels.Length || data.Length == 0)
            {
                throw new ArgumentException("Argumentos inválidos ou nulos fornecidos.");
            }

            double total = data.Sum();
            double startAngle = 0;

            // Cores para as fatias do gráfico
            XColor[] sliceColors = { XColors.CornflowerBlue, XColors.LightGreen, XColors.SkyBlue, XColors.Gold, XColors.OrangeRed, XColors.MediumPurple };

            for (int i = 0; i < data.Length; i++)
            {
                double sweepAngle = 360 * (data[i] / total);

                // Seleciona uma cor para a fatia
                XBrush brush = new XSolidBrush(sliceColors[i % sliceColors.Length]);

                // Desenha a fatia do gráfico
                gfx.DrawPie(brush, (float)(centerX - radius), (float)(centerY - radius), (float)(2 * radius), (float)(2 * radius), (float)startAngle, (float)sweepAngle);

                // Calcula a posição do rótulo
                double labelX = centerX + (radius * 0.5) * Math.Cos(Math.PI * (startAngle + sweepAngle / 2) / 180);
                double labelY = centerY + (radius * 0.5) * Math.Sin(Math.PI * (startAngle + sweepAngle / 2) / 180);

                // Adiciona um rótulo com uma cor de texto legível e uma sombra para melhor visibilidade
                XFont font = new XFont("Arial", 10, XFontStyle.Bold);
                XBrush textBrush = XBrushes.Black;
                XBrush shadowBrush = new XSolidBrush(XColors.LightGray);

                // Adiciona a sombra
                gfx.DrawString($"{labels[i]} ({data[i]:F2}%)", font, shadowBrush, new XPoint(labelX + 1, labelY + 1), XStringFormats.Center);

                // Adiciona o rótulo
                gfx.DrawString($"{labels[i]} ({data[i]:F2}%)", font, textBrush, new XPoint(labelX, labelY), XStringFormats.Center);

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

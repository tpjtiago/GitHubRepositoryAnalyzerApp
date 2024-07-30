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
            PdfDocument document = new PdfDocument();
            PdfPage page = document.AddPage();
            XGraphics gfx = XGraphics.FromPdfPage(page);

            XFont titleFont = new XFont("Arial", 24, XFontStyle.Bold);
            XFont subtitleFont = new XFont("Arial", 14, XFontStyle.Italic);
            XFont font = new XFont("Arial", 12, XFontStyle.Regular);
            XFont sectionTitleFont = new XFont("Arial", 14, XFontStyle.Bold);
            XFont barFont = new XFont("Arial", 10, XFontStyle.Bold);
            XBrush titleBrush = XBrushes.LightCyan;
            XBrush subtitleBrush = XBrushes.LightSteelBlue;
            XFont tagFont = new XFont("Arial", 10, XFontStyle.Bold);
            XFont tagDescriptionFont = new XFont("Arial", 10, XFontStyle.Regular);

            double headerHeight = 100;
            double chartWidth = 200;
            double chartHeight = 200;
            double margin = 20;
            double sectionMarginTop = 30;
            double sectionSpacing = 50;
            double barHeight = 20;

            // Draw header background
            gfx.DrawRectangle(XBrushes.DarkSlateBlue, 0, 0, page.Width, headerHeight);
            gfx.DrawLine(new XPen(XColors.WhiteSmoke, 2), 20, headerHeight, page.Width, headerHeight);

            // Draw title and subtitle
            gfx.DrawString("Relatório de Utilização de Inteligência Artificial",
                titleFont, titleBrush,
                new XRect(0, 0, page.Width, headerHeight - 40),
                XStringFormats.Center);

            gfx.DrawString("Análise detalhada dos dados e insights",
                subtitleFont, XBrushes.White,
                new XRect(0, 40, page.Width, headerHeight - 60),
                XStringFormats.Center);

            string dataInicial = "01/04/2024";
            string dataFinal = "20/04/2024";
            XSize size = gfx.MeasureString($"Período: {dataInicial} a {dataFinal}", font);
            gfx.DrawString($"Período: {dataInicial} a {dataFinal}", font, XBrushes.LightSteelBlue, new XRect(0, headerHeight - 25, page.Width, size.Height), XStringFormats.Center);

            // Deserialize JSON response
            using (JsonDocument doc = JsonDocument.Parse(responseBody))
            {
                var commits = doc.RootElement.EnumerateArray();

                int totalCommits = 0;
                int copilotCommits = 0;
                Dictionary<string, int> authorCommitCounts = new Dictionary<string, int>();
                Dictionary<string, int> tagCommitCounts = new Dictionary<string, int>();

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

                    if (commitMessage.Contains("ai", StringComparison.OrdinalIgnoreCase))
                    {
                        copilotCommits++;
                    }

                    string[] tags = { "feat", "fix", "docs", "style", "refactor", "test", "chore", "ai" };
                    foreach (var tag in tags)
                    {
                        if (commitMessage.Contains(tag, StringComparison.OrdinalIgnoreCase))
                        {
                            if (!tagCommitCounts.ContainsKey(tag))
                            {
                                tagCommitCounts[tag] = 0;
                            }
                            tagCommitCounts[tag]++;
                        }
                    }
                }

                double copilotPercentage = totalCommits > 0 ? (double)copilotCommits / totalCommits * 100 : 0;

                // Draw data
                gfx.DrawString($"Total de commits: {totalCommits}", font, XBrushes.Black, new XRect(30, headerHeight + 20, page.Width, size.Height), XStringFormats.TopLeft);
                gfx.DrawString($"Commits com Tag 'IA': {copilotCommits}", font, XBrushes.Black, new XRect(30, headerHeight + 40, page.Width, size.Height), XStringFormats.TopLeft);
                gfx.DrawString($"Porcentagem de uso de IA: {copilotPercentage.ToString("F2")}%", font, XBrushes.Black, new XRect(30, headerHeight + 60, page.Width, size.Height), XStringFormats.TopLeft);

                // Draw Pie Chart
                double pieChartX = page.Width - chartWidth - margin;
                double pieChartY = headerHeight + 80;
                double[] pieChartData = { copilotPercentage, 100 - copilotPercentage };
                string[] pieChartLabels = { "IA", "Outros" };
                DrawPieChart(gfx, pieChartData, pieChartLabels, pieChartX + chartWidth / 2, pieChartY + chartHeight / 2, chartWidth / 2);

                // Draw Author Bar Chart
                double authorBarChartX = 30;
                double authorBarChartY = headerHeight + 120;
                double authorBarChartWidth = page.Width - 2 * margin - chartWidth - 30;

                gfx.DrawString("Commits por Autor", sectionTitleFont, XBrushes.Black, new XRect(authorBarChartX, authorBarChartY - 30, authorBarChartWidth, barHeight), XStringFormats.TopLeft);
                DrawBarChart(gfx, authorCommitCounts, authorBarChartX, authorBarChartY, authorBarChartWidth, barHeight, true);

                // Draw Tag Bar Chart
                double tagBarChartY = authorBarChartY + authorCommitCounts.Count * (barHeight + 10) + 30;

                gfx.DrawString("Commits por Tag", sectionTitleFont, XBrushes.Black, new XRect(authorBarChartX, tagBarChartY - 30, authorBarChartWidth, barHeight), XStringFormats.TopLeft);
                DrawBarChart(gfx, tagCommitCounts.OrderByDescending(x => x.Value).ToDictionary(x => x.Key, x => x.Value), authorBarChartX, tagBarChartY, authorBarChartWidth, barHeight, false);

                // Draw Tag References
                double referenceY = tagBarChartY + tagCommitCounts.Count * (barHeight + 10) + 50;
                gfx.DrawString("Referências de Tags:", sectionTitleFont, XBrushes.Black, new XRect(authorBarChartX, referenceY, authorBarChartWidth, barHeight), XStringFormats.TopLeft);

                double referenceStartY = referenceY + 20;
                double referenceSpacing = 20;

                var tagReferences = new Dictionary<string, string>
                {
                    { "ai", "Para commits que utilizam IA" },
                    { "feat", "Novos recursos" },
                    { "fix", "Correções de bugs" },
                    { "docs", "Documentação" },
                    { "style", "Formatação, estilos" },
                    { "refactor", "Refatoração de código" },
                    { "perf", "Melhorias de performance" },
                    { "test", "Testes" },
                    { "chore", "Tarefas de manutenção" },
                    { "ci", "Integração contínua" }
                };

                foreach (var tag in tagReferences)
                {
                    gfx.DrawString($"{tag.Key}", tagFont, XBrushes.Black, new XRect(authorBarChartX, referenceStartY, authorBarChartWidth, barHeight), XStringFormats.TopLeft);
                    gfx.DrawString($" - {tag.Value}", tagDescriptionFont, XBrushes.Gray, new XRect(authorBarChartX + 40, referenceStartY, authorBarChartWidth, barHeight), XStringFormats.TopLeft);
                    referenceStartY += referenceSpacing;
                }
            }

            // Save the PDFF
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

            XColor[] sliceColors = { XColors.CornflowerBlue, XColors.LightGreen, XColors.SkyBlue, XColors.Gold, XColors.OrangeRed };
            for (int i = 0; i < data.Length; i++)
            {
                double sweepAngle = 360 * (data[i] / total);
                XBrush brush = new XSolidBrush(sliceColors[i % sliceColors.Length]);
                gfx.DrawPie(brush, (float)(centerX - radius), (float)(centerY - radius), (float)(2 * radius), (float)(2 * radius), (float)startAngle, (float)sweepAngle);

                double labelX = centerX + (radius * 0.5) * Math.Cos(Math.PI * (startAngle + sweepAngle / 2) / 180);
                double labelY = centerY + (radius * 0.5) * Math.Sin(Math.PI * (startAngle + sweepAngle / 2) / 180);

                XFont font = new XFont("Arial", 10, XFontStyle.Bold);
                XBrush textBrush = XBrushes.Black;
                XBrush shadowBrush = new XSolidBrush(XColors.LightGray);

                gfx.DrawString($"{labels[i]} ({data[i]:F2}%)", font, shadowBrush, new XPoint(labelX + 1, labelY + 1), XStringFormats.Center);
                gfx.DrawString($"{labels[i]} ({data[i]:F2}%)", font, textBrush, new XPoint(labelX, labelY), XStringFormats.Center);

                startAngle += sweepAngle;
            }
        }

        static void DrawBarChart(XGraphics gfx, Dictionary<string, int> commitCounts, double startX, double startY, double barWidth, double barHeight, bool includeValue)
        {
            if (commitCounts == null || !commitCounts.Any())
            {
                throw new ArgumentException("Nenhum dado de commit fornecido.");
            }

            int maxCount = commitCounts.Values.Max();
            double scaleFactor = barWidth / maxCount;

            int i = 0;
            foreach (var pair in commitCounts)
            {
                double barLength = pair.Value * scaleFactor;
                double barX = startX;
                double barY = startY + i * (barHeight + 10);

                gfx.DrawRectangle(XBrushes.LightBlue, barX, barY, barLength, barHeight);

                // Draw text inside the bar
                string text = $"{pair.Key} ({pair.Value})";
                XFont font = new XFont("Arial", 10, XFontStyle.Bold);
                XBrush textBrush = XBrushes.Black;
                gfx.DrawString(text, font, textBrush, barX + barLength / 2, barY + barHeight / 2, XStringFormats.Center);

                i++;
            }
        }
    }
}

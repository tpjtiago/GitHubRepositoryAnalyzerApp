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
            string repositoryOwner = "";
            string repositoryName = "";
            string accessToken = "";

            string apiUrl = $"https://api.github.com/repos/{repositoryOwner}/{repositoryName}/commits?sha=develop";
            List<JsonElement> allCommits = new List<JsonElement>();

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("GitHubRepositoryAnalyzer", "1.0"));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Token", accessToken);

                bool hasMorePages = true;
                int page = 1;

                while (hasMorePages)
                {
                    HttpResponseMessage response = await client.GetAsync($"{apiUrl}&page={page}");

                    if (response.IsSuccessStatusCode)
                    {
                        string responseBody = await response.Content.ReadAsStringAsync();
                        using (JsonDocument doc = JsonDocument.Parse(responseBody))
                        {
                            foreach (var element in doc.RootElement.EnumerateArray())
                            {
                                allCommits.Add(element.Clone());
                            }
                        }

                        // Check if there are more pages
                        if (response.Headers.Contains("Link"))
                        {
                            var linkHeader = response.Headers.GetValues("Link").FirstOrDefault();
                            hasMorePages = linkHeader != null && linkHeader.Contains("rel=\"next\"");
                        }
                        else
                        {
                            hasMorePages = false;
                        }

                        page++;
                    }
                    else
                    {
                        Console.WriteLine($"Falha na solicitação: {response.StatusCode}");
                        hasMorePages = false;
                    }
                }

                // Process all commits
                CreatePdfWithCommitData(allCommits);
            }
        }

        static void CreatePdfWithCommitData(List<JsonElement> commits)
        {
            PdfDocument pdfDocument = new PdfDocument();
            PdfPage page = pdfDocument.AddPage();
            XGraphics gfx = XGraphics.FromPdfPage(page);

            XFont titleFont = new XFont("Arial", 20, XFontStyle.Bold);
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
            double barHeight = 20;

            // Draw header background
            gfx.DrawRectangle(XBrushes.DarkSlateBlue, 0, 0, page.Width, headerHeight);
            gfx.DrawLine(new XPen(XColors.WhiteSmoke, 2), 20, headerHeight, page.Width, headerHeight);

            // Draw title and subtitle
            gfx.DrawString("Relatório de Utilização de TAGs",
                titleFont, titleBrush,
                new XRect(0, 0, page.Width, headerHeight - 40),
                XStringFormats.Center);

            gfx.DrawString("TD Gift card Web",
                subtitleFont, XBrushes.White,
                new XRect(0, 40, page.Width, headerHeight - 60),
                XStringFormats.Center);

            string dataInicial = "27/06/2024";
            string dataFinal = "22/10/2024";
            XSize size = gfx.MeasureString($"Período: {dataInicial} a {dataFinal}", font);
            gfx.DrawString($"Análise detalhada dos dados e insights do período: {dataInicial} a {dataFinal}", font, XBrushes.LightSteelBlue, new XRect(0, headerHeight - 25, page.Width, size.Height), XStringFormats.Center);

            int totalCommits = commits.Count;
            int copilotCommits = 0;
            Dictionary<string, int> authorCommitCounts = new Dictionary<string, int>();
            Dictionary<string, int> tagCommitCounts = new Dictionary<string, int>();

            foreach (var commit in commits)
            {
                string? authorName = commit.GetProperty("commit").GetProperty("author").GetProperty("email").GetString();
                string? commitMessage = commit.GetProperty("commit").GetProperty("message").GetString();

                if (authorName != null)
                {
                    if (!authorCommitCounts.ContainsKey(authorName))
                    {
                        authorCommitCounts[authorName] = 0;
                    }
                    authorCommitCounts[authorName]++;
                }

                if (commitMessage != null)
                {
                    string[] tags = { "feat", "fix", "docs", "style", "test", "chore", "ai", "ia" };
                    bool hasTag = false;

                    foreach (var tag in tags)
                    {
                        if (commitMessage.Contains(tag, StringComparison.OrdinalIgnoreCase))
                        {
                            string normalizedTag = tag == "ia" ? "ai" : tag; // Normalize "ia" to "ai"

                            if (!tagCommitCounts.ContainsKey(normalizedTag))
                            {
                                tagCommitCounts[normalizedTag] = 0;
                            }
                            tagCommitCounts[normalizedTag]++;
                            hasTag = true;
                        }
                    }

                    if (hasTag)
                    {
                        copilotCommits++;
                    }
                }
            }


            double copilotPercentage = totalCommits > 0 ? (double)copilotCommits / totalCommits * 100 : 0;

            // Draw data
            gfx.DrawString($"Total de commits: {totalCommits}", font, XBrushes.Black, new XRect(30, headerHeight + 20, page.Width, size.Height), XStringFormats.TopLeft);
            gfx.DrawString($"Commits com Tag: {copilotCommits}", font, XBrushes.Black, new XRect(30, headerHeight + 40, page.Width, size.Height), XStringFormats.TopLeft);
            gfx.DrawString($"Porcentagem de uso: {copilotPercentage.ToString("F2")}%", font, XBrushes.Black, new XRect(30, headerHeight + 60, page.Width, size.Height), XStringFormats.TopLeft);

            // Calculate percentages for pie chart
            double[] pieChartData = tagCommitCounts.Values.Select(v => (double)v / totalCommits * 100).ToArray();
            string[] pieChartLabels = tagCommitCounts.Keys.ToArray();

            // Draw Pie Chart
            double pieChartX = page.Width - chartWidth - margin;
            double pieChartY = headerHeight + 80;
            DrawPieChart(gfx, pieChartData, pieChartLabels, pieChartX + chartWidth / 2, pieChartY + chartHeight / 2, chartWidth / 2);

            // Draw Author Bar Chart
            double tagBarChartY = headerHeight + 120;
            double authorBarChartWidth = page.Width - 2 * margin - chartWidth - 30;
            double authorBarChartWidth2 = page.Width - 2 * margin - chartWidth - 30 + 200; 

            gfx.DrawString("Commits por Tag", sectionTitleFont, XBrushes.Black, new XRect(30, tagBarChartY - 40, authorBarChartWidth, barHeight), XStringFormats.TopLeft);
            DrawBarChart(gfx, tagCommitCounts.OrderByDescending(x => x.Value).ToDictionary(x => x.Key, x => x.Value), 30, tagBarChartY, authorBarChartWidth, barHeight, true, "Tag");

            // Calculate the height of the Tag Bar Chart
            double tagBarChartHeight = tagCommitCounts.Count * (barHeight + 10);

            // Draw Author Bar Chart
            double authorBarChartY = tagBarChartY + tagBarChartHeight + 25; //Ajuste de posição

            gfx.DrawString("Commits por Autor", sectionTitleFont, XBrushes.Black, new XRect(30, authorBarChartY - 30, authorBarChartWidth, barHeight), XStringFormats.TopLeft);
            DrawBarChart(gfx, authorCommitCounts.OrderByDescending(x => x.Value).ToDictionary(x => x.Key, x => x.Value), 30, authorBarChartY, authorBarChartWidth2, barHeight, true, "E-mail");

            // Calculate the height of the Author Bar Chart
            double authorBarChartHeight = authorCommitCounts.Count * (barHeight + 10);

            // Draw Tag References
            double referenceY = authorBarChartY + authorBarChartHeight - 80; //Ajuste de posição
            gfx.DrawString("Referências de Tags:", sectionTitleFont, XBrushes.Black, new XRect(30, referenceY, authorBarChartWidth, barHeight), XStringFormats.TopLeft);

            double referenceStartY = referenceY + 30;
            double referenceSpacing = 18;

            var tagReferences = new Dictionary<string, string>
            {
                { "ai", "Para commits que utilizam IA" },
                { "feat", "Novos recursos" },
                { "fix", "Correções de bugs" },
                { "test", "Testes" },
                { "chore", "Tarefas de manutenção" },
                { "refactor", "Refatoração de código" },
                { "style", "Formatação, estilos" }
            };

            foreach (var tag in tagReferences)
            {
                gfx.DrawString($"{tag.Key}", tagFont, XBrushes.Black, new XRect(30, referenceStartY, authorBarChartWidth, barHeight), XStringFormats.TopLeft);
                gfx.DrawString($" - {tag.Value}", tagDescriptionFont, XBrushes.Gray, new XRect(70, referenceStartY, authorBarChartWidth, barHeight), XStringFormats.TopLeft);
                referenceStartY += referenceSpacing;
            }

            // Save the PDF with a dynamic name
            string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
            string pdfPath = $@"C:\PDF-Git\Arquivo_{timestamp}.pdf";
            pdfDocument.Save(pdfPath);

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

            XColor[] sliceColors = { XColors.CornflowerBlue, XColors.LightGreen, XColors.SkyBlue, XColors.Gold, XColors.OrangeRed, XColors.Purple, XColors.Pink, XColors.Brown };
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

                double percentage = (data[i] / total) * 100;
                gfx.DrawString($"{labels[i]} ({percentage:F2}%)", font, shadowBrush, new XPoint(labelX + 1, labelY + 1), XStringFormats.Center);
                gfx.DrawString($"{labels[i]} ({percentage:F2}%)", font, textBrush, new XPoint(labelX, labelY), XStringFormats.Center);

                startAngle += sweepAngle;
            }
        }

        static void DrawBarChart(XGraphics gfx, Dictionary<string, int> commitCounts, double startX, double startY, double tableWidth, double rowHeight, bool includeValue, string columnName)
        {
            if (commitCounts == null || !commitCounts.Any())
            {
                throw new ArgumentException("Nenhum dado de commit fornecido.");
            }

            XFont headerFont = new XFont("Arial", 12, XFontStyle.Bold);
            XFont cellFont = new XFont("Arial", 10, XFontStyle.Regular);
            XBrush headerBrush = XBrushes.LightGray;
            XBrush cellBrush = XBrushes.White;
            XBrush textBrush = XBrushes.Black;

            double tableHeight = (commitCounts.Count + 1) * rowHeight;
            double columnWidth = tableWidth / (includeValue ? 3 : 2);

            // Draw table header
            gfx.DrawRectangle(headerBrush, startX, startY, tableWidth, rowHeight);
            gfx.DrawString(columnName, headerFont, textBrush, new XRect(startX, startY, columnWidth, rowHeight), XStringFormats.Center);
            gfx.DrawString("Count", headerFont, textBrush, new XRect(startX + columnWidth, startY, columnWidth, rowHeight), XStringFormats.Center);
            if (includeValue)
            {
                gfx.DrawString("Percentage", headerFont, textBrush, new XRect(startX + 2 * columnWidth, startY, columnWidth, rowHeight), XStringFormats.Center);
            }

            // Draw table rows
            int totalCommits = commitCounts.Values.Sum();
            int i = 0;
            foreach (var pair in commitCounts)
            {
                double rowY = startY + (i + 1) * rowHeight;
                gfx.DrawRectangle(cellBrush, startX, rowY, tableWidth, rowHeight);
                gfx.DrawString(pair.Key, cellFont, textBrush, new XRect(startX, rowY, columnWidth, rowHeight), XStringFormats.Center);
                gfx.DrawString(pair.Value.ToString(), cellFont, textBrush, new XRect(startX + columnWidth, rowY, columnWidth, rowHeight), XStringFormats.Center);
                if (includeValue)
                {
                    double percentage = (double)pair.Value / totalCommits * 100;
                    gfx.DrawString($"{percentage:F2}%", cellFont, textBrush, new XRect(startX + 2 * columnWidth, rowY, columnWidth, rowHeight), XStringFormats.Center);
                }
                i++;
            }

            // Draw table borders
            gfx.DrawRectangle(XPens.Black, startX, startY, tableWidth, tableHeight);
            for (int j = 0; j <= commitCounts.Count; j++)
            {
                double rowY = startY + j * rowHeight;
                gfx.DrawLine(XPens.Black, startX, rowY, startX + tableWidth, rowY);
            }
            for (int k = 0; k <= (includeValue ? 3 : 2); k++)
            {
                double colX = startX + k * columnWidth;
                gfx.DrawLine(XPens.Black, colX, startY, colX, startY + tableHeight);
            }
        }


    }
}

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
            string accessToken = "seu_token_de_acesso_pessoal";

            string apiUrl = $"https://api.github.com/repos/{repositoryOwner}/{repositoryName}/commits";

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("GitHubRepositoryAnalyzer", "1.0"));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Token", accessToken);

                HttpResponseMessage response = await client.GetAsync(apiUrl);

                if (response.IsSuccessStatusCode)
                {
                    string responseBody = await response.Content.ReadAsStringAsync();

                    // Deserialize o JSON da resposta
                    var commits = JsonSerializer.Deserialize<dynamic[]>(responseBody);

                    int totalCommits = commits.Length;
                    int copilotCommits = 0;

                    foreach (var commit in commits)
                    {
                        string commitMessage = commit.commit.message;
                        if (commitMessage.Contains("copilot", StringComparison.OrdinalIgnoreCase))
                        {
                            copilotCommits++;
                        }
                    }

                    double copilotPercentage = totalCommits > 0 ? (double)copilotCommits / totalCommits * 100 : 0;

                    Console.WriteLine($"Total de commits: {totalCommits}");
                    Console.WriteLine($"Commits com a palavra 'copilot': {copilotCommits}");
                    Console.WriteLine($"Porcentagem de uso de IA: {copilotPercentage}%");
                }
                else
                {
                    Console.WriteLine($"Falha na solicitação: {response.StatusCode}");
                }
            }
        }
    }
}

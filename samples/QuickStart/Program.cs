using System;
using System.Threading.Tasks;
using Arcademia.Leaderboards;

internal static class Program
{
    private static async Task Main()
    {
        Console.WriteLine("Arcademia Leaderboards Quick Start");
        Console.WriteLine(
            "Mode: " + ArcademiaLeaderboards.Mode
            + (ArcademiaLeaderboards.Mode == ArcademiaMode.Launcher
                ? "  |  session " + ArcademiaLeaderboards.SessionId
                : "  |  sandbox (Test area only)"));

        var apiBase = Prompt("API base", ArcademiaLeaderboards.Settings.apiBase);
        var apiKey = Prompt("API key", ArcademiaLeaderboards.Settings.apiKey);
        var boardSlug = Prompt("Board slug", "highscore");

        ArcademiaLeaderboards.Configure(new ArcademiaSettings { apiBase = apiBase, apiKey = apiKey });

        string lastScoreId = null;
        while (true)
        {
            Console.WriteLine();
            Console.WriteLine("1) Ping  2) Submit random score  3) Fetch test scores  4) Claim last score  5) Quit");
            Console.Write("> ");
            switch (Console.ReadLine())
            {
                case "1":
                    Console.WriteLine((await ArcademiaLeaderboards.PingAsync()).ToString());
                    break;
                case "2":
                    var value = new Random().Next(100, 100000);
                    var result = await ArcademiaLeaderboards.SubmitScoreAsync(boardSlug, value, "REX");
                    if (result.Success && !string.IsNullOrEmpty(result.ScoreId))
                        lastScoreId = result.ScoreId;
                    Console.WriteLine(result.ToString());
                    break;
                case "3":
                    var scores = await ArcademiaLeaderboards.GetTestScoresAsync(boardSlug, 10, 0);
                    if (!scores.Success)
                    {
                        Console.WriteLine("Failed - " + scores.Message);
                        break;
                    }
                    Console.WriteLine($"{scores.BoardName} ({scores.Total} test scores)");
                    foreach (var s in scores.Scores)
                        Console.WriteLine($"    #{s.Rank}  {s.PlayerName}  {s.Value}");
                    break;
                case "4":
                    if (string.IsNullOrEmpty(lastScoreId))
                    {
                        Console.WriteLine("Submit a score first.");
                        break;
                    }
                    Console.WriteLine("Requesting claim for " + lastScoreId + "...");
                    Console.WriteLine((await ArcademiaLeaderboards.RequestClaimAsync(lastScoreId)).ToString());
                    break;
                case "5":
                    return;
            }
        }
    }

    private static string Prompt(string label, string current)
    {
        Console.Write($"{label} [{current}]: ");
        var input = Console.ReadLine();
        return string.IsNullOrWhiteSpace(input) ? current : input;
    }
}

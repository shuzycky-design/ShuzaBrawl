
using System.Security.Cryptography;
using DeenGames.Utils.AStarPathFinder;
using Newtonsoft.Json;
using IndusBrawl.Laser.Server;
// thank wisedev
class RefreshFingerprint
{
    public static void Main()
    {
        string directoryPath = "PatchAssets";
        string outputPath = "fingerprint.json";
        
        string version = "53.301.1"; // hardcoded for now

        Logger.Print("Creating fingerprint.json...");

        var files = new List<object>();
        using (SHA1 sha1 = SHA1.Create())
        {
            foreach (string filePath in Directory.GetFiles(directoryPath, "*", SearchOption.AllDirectories))
            {
                string relativePath = Path.GetRelativePath(directoryPath, filePath);
                string hash = ComputeSHA1(sha1, filePath);
                files.Add(new { sha = hash, file = relativePath.Replace("\\", "/") });
            }
        }

        string totalSha = ComputeTotalSHA(files);
        var jsonData = new { files, sha = totalSha, version };
        File.WriteAllText(outputPath, JsonConvert.SerializeObject(jsonData, Newtonsoft.Json.Formatting.Indented));

        Logger.Print($"File {outputPath} created! ");
    }

    static string ComputeSHA1(SHA1 sha1, string filePath)
    {
        using (FileStream stream = File.OpenRead(filePath))
        {
            return BitConverter.ToString(sha1.ComputeHash(stream)).Replace("-", "").ToLower();
        }
    }

    static string ComputeTotalSHA(List<object> files)
    {
        using (SHA1 sha1 = SHA1.Create())
        {
            string concatHashes = string.Join("", files.Select(f => ((dynamic)f).sha));
            byte[] hashBytes = sha1.ComputeHash(System.Text.Encoding.UTF8.GetBytes(concatHashes));
            return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
        }
    }
}

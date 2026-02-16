using System;
using System.IO;
using System.Runtime.CompilerServices; 
using Newtonsoft.Json;

namespace HashSpec.Core;

public static class HashSpec
{
    /// <summary>
    /// Verifies the current state against a stored baseline using the calling method's name.
    /// </summary>
    public static void Verify(
        object currentState, 
        [CallerMemberName] string specName = "")
    {
        Verify(specName, currentState);
    }

    /// <summary>
    /// Verifies the current state against a stored baseline using a custom specification name.
    /// </summary>
    public static void Verify(string specName, object currentState)
    {
        if (string.IsNullOrEmpty(specName)) throw new ArgumentException("Spec name cannot be empty.");
        
        string actualHash = DeterministicHasher.CreateFingerprint(currentState);

        // 1. Path Setup (Source-aware)
        // Note: This logic assumes a standard project structure where 'Specs' lives in the project root.
        string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        var projectDir = Directory.GetParent(baseDir)!.Parent!.Parent!.Parent!;
        string specFolder = Path.Combine(projectDir.FullName, "Specs");
        string hashPath = Path.Combine(specFolder, $"{specName}.hash");
        string jsonPath = Path.Combine(specFolder, $"{specName}.json");

        // 2. The Healer Logic: Check if we are forcing an update
        bool updateMode = Environment.GetEnvironmentVariable("HASHSPEC_UPDATE") == "true";

        // 3. Handle First Run OR Update Request
        if (!File.Exists(hashPath) || updateMode)
        {
            Directory.CreateDirectory(specFolder);
            File.WriteAllText(hashPath, actualHash);
            File.WriteAllText(jsonPath, JsonConvert.SerializeObject(currentState, Formatting.Indented));

            string action = updateMode ? "Updated" : "Created new";
            Console.WriteLine($"[HashSpec] {action} baseline for: {specName}");
            return;
        }

        // 4. Standard Comparison
        string expectedHash = File.ReadAllText(hashPath);

        if (actualHash != expectedHash)
        {
            string actualJson = JsonConvert.SerializeObject(currentState, Formatting.Indented);
            
            // Try to load the expected JSON for the diff; if missing, we just show the hash mismatch
            string expectedJson = File.Exists(jsonPath) ? File.ReadAllText(jsonPath) : "{ \"Error\": \"Baseline JSON missing\" }";

            Console.WriteLine($"\n\u001b[31m[HashSpec] MISMATCH DETECTED in {specName}\u001b[0m");
            Console.WriteLine("--------------------------------------------------");

            // QUICK DIFF LOGIC
            var expectedLines = expectedJson.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            var actualLines = actualJson.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

            for (int i = 0; i < Math.Max(expectedLines.Length, actualLines.Length); i++)
            {
                string exp = i < expectedLines.Length ? expectedLines[i].Trim() : "";
                string act = i < actualLines.Length ? actualLines[i].Trim() : "";

                if (exp != act)
                {
                    if (!string.IsNullOrEmpty(exp)) Console.WriteLine($"\u001b[31m- {exp}\u001b[0m"); // Red
                    if (!string.IsNullOrEmpty(act)) Console.WriteLine($"\u001b[32m+ {act}\u001b[0m"); // Green
                }
            }
            Console.WriteLine("--------------------------------------------------");

            // 5. Save failure artifact and THROW
            string artifactDir = Path.Combine(Directory.GetCurrentDirectory(), "Artifacts");
            Directory.CreateDirectory(artifactDir);
            File.WriteAllText(Path.Combine(artifactDir, $"{specName}.actual.json"), actualJson);

            throw new Exception($"HashSpec Mismatch for '{specName}'! View the diff above or check the Artifacts folder.");
        }
    }
}
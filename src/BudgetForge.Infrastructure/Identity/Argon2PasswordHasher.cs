using System;
using System.Security.Cryptography;
using System.Text;
using Konscious.Security.Cryptography;
using Microsoft.AspNetCore.Identity;

namespace BudgetForge.Infrastructure.Identity
{
    /// <summary>
    /// Argon2id password hasher using PHC string format:
    /// $argon2id$v=19$m=65536,t=3,p=2$<salt_b64>$<hash_b64>
    /// </summary>
    public sealed class Argon2IdPasswordHasher<TUser> : IPasswordHasher<TUser> where TUser : class
    {
        // Reasonable defaults for dev on a laptop. You can tune later.
        private const int DefaultIterations = 3;          // t
        private const int DefaultMemoryKb  = 64 * 1024;   // m = 64MB
        private const int DefaultParallel  = 2;           // p
        private const int SaltBytes        = 16;
        private const int HashBytes        = 32;

        public string HashPassword(TUser user, string password)
        {
            if (password is null) throw new ArgumentNullException(nameof(password));

            // generate salt
            var salt = RandomNumberGenerator.GetBytes(SaltBytes);

            var hash = Hash(password, salt,
                iterations: DefaultIterations,
                memoryKb:  DefaultMemoryKb,
                degreeOfParallelism: Math.Max(1, Math.Min(Environment.ProcessorCount, DefaultParallel)),
                outputLength: HashBytes);

            // PHC string
            var phc = $"$argon2id$v=19$m={DefaultMemoryKb},t={DefaultIterations},p={DefaultParallel}$" +
                      $"{Convert.ToBase64String(salt)}$" +
                      $"{Convert.ToBase64String(hash)}";

            return phc;
        }

        public PasswordVerificationResult VerifyHashedPassword(TUser user, string hashedPassword, string providedPassword)
        {
            if (hashedPassword is null) throw new ArgumentNullException(nameof(hashedPassword));
            if (providedPassword is null) throw new ArgumentNullException(nameof(providedPassword));

            // Expect PHC format. If not, we can’t verify.
            if (!hashedPassword.StartsWith("$argon2id$", StringComparison.Ordinal))
                return PasswordVerificationResult.Failed;

            try
            {
                // Example: $argon2id$v=19$m=65536,t=3,p=2$<salt>$<hash>
                var parts = hashedPassword.Split('$', StringSplitOptions.RemoveEmptyEntries);
                // parts[0] = "argon2id"
                // parts[1] = "v=19"
                // parts[2] = "m=...,t=...,p=..."
                // parts[3] = salt (b64)
                // parts[4] = hash (b64)
                if (parts.Length != 5) return PasswordVerificationResult.Failed;

                var paramPart = parts[2];
                int m = GetParam(paramPart, "m");
                int t = GetParam(paramPart, "t");
                int p = GetParam(paramPart, "p");

                var salt = Convert.FromBase64String(parts[3]);
                var expected = Convert.FromBase64String(parts[4]);

                var actual = Hash(providedPassword, salt,
                    iterations: t,
                    memoryKb:  m,
                    degreeOfParallelism: p,
                    outputLength: expected.Length);

                // constant-time compare
                if (!CryptographicOperations.FixedTimeEquals(expected, actual))
                    return PasswordVerificationResult.Failed;

                // If parameters changed (you tuned them later), you can trigger rehash here.
                // For now, just return Success.
                return PasswordVerificationResult.Success;
            }
            catch
            {
                return PasswordVerificationResult.Failed;
            }
        }

        private static int GetParam(string paramPart, string key)
        {
            foreach (var kv in paramPart.Split(',', StringSplitOptions.RemoveEmptyEntries))
            {
                var pair = kv.Split('=', StringSplitOptions.RemoveEmptyEntries);
                if (pair.Length == 2 && pair[0] == key && int.TryParse(pair[1], out var val))
                    return val;
            }
            throw new FormatException($"Missing Argon2 parameter '{key}'.");
        }

        private static byte[] Hash(string password, byte[] salt, int iterations, int memoryKb, int degreeOfParallelism, int outputLength)
        {
            var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password))
            {
                Salt = salt,
                Iterations = iterations,
                MemorySize = memoryKb,
                DegreeOfParallelism = degreeOfParallelism
            };

            return argon2.GetBytes(outputLength);
        }
    }
}
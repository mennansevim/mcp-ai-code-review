using System;

namespace SampleApp
{
    public static class Program
    {
        public static void Main()
        {
            Console.WriteLine("Hello, world! Miray Sevim");
            
            // CRITICAL SECURITY TEST - Please review!
            
            // Test AI review
            var result = CalculateDiscount(100);
            Console.WriteLine($"Discount: {result}");
            
            // Test database connection
            var conn = GetDatabaseConnection();
            Console.WriteLine($"Connected: {conn != null}");
        }
        
        // SQL Injection vulnerability for testing
        public static string GetUserData(string userId)
        {
            // CRITICAL: SQL Injection vulnerability!
            string query = "SELECT * FROM Users WHERE Id = '" + userId + "'";
            return query; // This is dangerous!
        }
        
        // Hardcoded credentials
        public static string GetDatabaseConnection()
        {
            // CRITICAL: Never hardcode database credentials!
            string connectionString = "Server=localhost;Database=mydb;User=admin;Password=Pass123!;";
            return connectionString;
        }
        
        // Intentional code issues for AI review testing
        public static double CalculateDiscount(double price)
        {
            // TODO: Remove hardcoded values
            double discount = 0;
            
            if (price > 100)
                discount = price * 0.2; // 20% discount
            else if (price > 50)
                discount = price * 0.1; // 10% discount
                
            // Potential issue: No null check, magic numbers
            return discount;
        }
        
        // Additional problematic code for testing
        public static string GetUserPassword(string username)
        {
            // Security issue: Hardcoded password
            if (username == "admin")
                return "admin123"; // CRITICAL: Never hardcode passwords!
            
            return "default";
        }
        
        // Path Traversal vulnerability
        public static string ReadUserFile(string fileName)
        {
            // CRITICAL: Path traversal attack possible!
            string filePath = "/var/data/users/" + fileName;
            // No validation - attacker could use ../../etc/passwd
            return System.IO.File.ReadAllText(filePath);
        }
        
        // Insecure random for security
        public static string GenerateToken()
        {
            // HIGH: Using non-cryptographic random for security token!
            var random = new Random();
            return random.Next(1000000).ToString();
        }
        
        // CRITICAL: Command Injection vulnerability
        public static void ExecuteCommand(string userInput)
        {
            // CRITICAL: Command injection attack possible!
            var process = new System.Diagnostics.Process();
            process.StartInfo.FileName = "/bin/bash";
            process.StartInfo.Arguments = "-c " + userInput; // No sanitization!
            process.Start();
        }
        
        // CRITICAL: XML External Entity (XXE) vulnerability
        public static void ParseXml(string xmlContent)
        {
            // CRITICAL: XXE attack possible - DtdProcessing enabled!
            var settings = new System.Xml.XmlReaderSettings();
            settings.DtdProcessing = System.Xml.DtdProcessing.Parse;
            settings.XmlResolver = new System.Xml.XmlUrlResolver();
            
            using var reader = System.Xml.XmlReader.Create(
                new System.IO.StringReader(xmlContent), settings);
            // XXE vulnerability!
        }
        
        // CRITICAL: Insecure Deserialization
        public static object DeserializeData(string data)
        {
            // CRITICAL: BinaryFormatter is dangerous - allows arbitrary code execution!
            var formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
            using var stream = new System.IO.MemoryStream(Convert.FromBase64String(data));
            return formatter.Deserialize(stream); // RCE vulnerability!
        }
        
        // CRITICAL: LDAP Injection
        public static string SearchUser(string username)
        {
            // CRITICAL: LDAP injection - user input directly in filter!
            string ldapFilter = $"(&(objectClass=user)(cn={username}))";
            // Attacker could inject: *)(objectClass=*))(&(cn=*
            return ldapFilter;
        }
        
        // CRITICAL: Server-Side Request Forgery (SSRF)
        public static async System.Threading.Tasks.Task<string> FetchUrl(string url)
        {
            // CRITICAL: SSRF - no URL validation, can access internal resources!
            using var client = new System.Net.Http.HttpClient();
            // Attacker could use: http://localhost:6379/ or http://169.254.169.254/
            return await client.GetStringAsync(url);
        }
        
        // HIGH: Weak Cryptography - MD5 for password hashing
        public static string HashPassword(string password)
        {
            // HIGH: MD5 is cryptographically broken - use bcrypt or PBKDF2!
            using var md5 = System.Security.Cryptography.MD5.Create();
            byte[] hashBytes = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(hashBytes);
            // MD5 can be cracked in seconds with rainbow tables!
        }
        
        // MEDIUM: Information Disclosure - exposing stack trace
        public static string ProcessRequest(string input)
        {
            try
            {
                // Some processing...
                if (string.IsNullOrEmpty(input))
                    throw new ArgumentException("Invalid input");
                return input.ToUpper();
            }
            catch (Exception ex)
            {
                // MEDIUM: Stack trace exposed to user - information disclosure!
                return $"Error: {ex.ToString()}";
                // Reveals internal paths, method names, framework versions!
            }
        }
        
        // HIGH: Insecure Random for Password Generation
        public static string GeneratePassword(int length)
        {
            // HIGH: Using Random() for security-critical operation!
            var random = new Random();
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var password = new char[length];
            
            for (int i = 0; i < length; i++)
            {
                password[i] = chars[random.Next(chars.Length)];
            }
            
            // Predictable - use RNGCryptoServiceProvider instead!
            return new string(password);
        }
        
        // MEDIUM: Excessive Logging - sensitive data in logs
        public static void LoginUser(string username, string password)
        {
            // MEDIUM: Logging sensitive data (password in plain text!)
            Console.WriteLine($"Login attempt: User={username}, Password={password}");
            // Password should NEVER be logged!
            
            // Authentication logic...
        }
        
        // HIGH: Race Condition - not thread-safe
        private static int _userCount = 0;
        
        public static void RegisterUser(string username)
        {
            // HIGH: Race condition - _userCount++ is not atomic!
            _userCount++;
            
            // Multiple threads can read same value, increment, write back
            // causing lost updates!
            Console.WriteLine($"Registered user #{_userCount}: {username}");
            
            // Should use: Interlocked.Increment(ref _userCount)
        }
    }
}

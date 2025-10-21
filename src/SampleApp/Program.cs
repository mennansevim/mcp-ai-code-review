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
    }
}

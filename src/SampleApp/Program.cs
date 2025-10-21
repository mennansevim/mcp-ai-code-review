using System;

namespace SampleApp
{
    public static class Program
    {
        public static void Main()
        {
            Console.WriteLine("Hello, world! Miray Sevim");
            
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
    }
}

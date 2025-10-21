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
    }
}

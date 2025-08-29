namespace BudgetForge.Application.DTOs
{
    public class MonthlyTotal
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public decimal Income { get; set; }
        public decimal Expense { get; set; }
    }

    public class CategoryTotal
    {
        public string Category { get; set; } = string.Empty;
        public decimal Amount { get; set; }
    }

    public class BudgetSummaryDto
    {
        public DateTime From { get; set; }
        public DateTime To   { get; set; }

        public decimal TotalIncome   { get; set; }
        public decimal TotalExpenses { get; set; }
        public decimal Net => TotalIncome - TotalExpenses;

        public List<CategoryTotal> TopCategories { get; set; } = new();
        public List<MonthlyTotal>  Monthly       { get; set; } = new();
    }
}
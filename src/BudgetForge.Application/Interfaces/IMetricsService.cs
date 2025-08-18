namespace BudgetForge.Application.Interfaces
{
    public interface IMetricsService
    {
        void IncrementUserCreated();
        void IncrementTransactionCreated();
    }
}
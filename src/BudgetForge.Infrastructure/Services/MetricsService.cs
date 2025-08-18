using BudgetForge.Application.Interfaces;
using Prometheus;

namespace BudgetForge.Infrastructure.Services
{
    public class MetricsService : IMetricsService
    {
        private readonly Counter _userCreatedCounter;
        private readonly Counter _transactionCreatedCounter;

        public MetricsService()
        {
            _userCreatedCounter = Metrics.CreateCounter("users_created_total", "Number of users created");
            _transactionCreatedCounter = Metrics.CreateCounter("transactions_created_total", "Number of transactions created");
        }

        public void IncrementUserCreated() => _userCreatedCounter.Inc();
        public void IncrementTransactionCreated() => _transactionCreatedCounter.Inc();
    }
}
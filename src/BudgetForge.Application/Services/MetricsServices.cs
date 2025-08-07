using Prometheus;

namespace BudgetForge.Application.Services
{
    public interface IMetricsService
    {
        void IncrementLoginAttempt(bool successful);
        void IncrementUserRegistration();
        void IncrementTokenRefresh();
        void RecordAuthenticationLatency(double seconds);
    }

    public class MetricsService : IMetricsService
    {
        // Define custom metrics
        private static readonly Counter LoginAttempts = Metrics
            .CreateCounter("budgetforge_login_attempts_total", "Total login attempts", new[] { "status" });

        private static readonly Counter UserRegistrations = Metrics
            .CreateCounter("budgetforge_user_registrations_total", "Total user registrations");

        private static readonly Counter TokenRefreshes = Metrics
            .CreateCounter("budgetforge_token_refreshes_total", "Total token refresh attempts");

        private static readonly Histogram AuthenticationDuration = Metrics
            .CreateHistogram("budgetforge_authentication_duration_seconds", "Authentication request duration");

        private static readonly Gauge ActiveUsers = Metrics
            .CreateGauge("budgetforge_active_users", "Number of active users");

        private static readonly Gauge DatabaseConnections = Metrics
            .CreateGauge("budgetforge_database_connections", "Number of active database connections");

        public void IncrementLoginAttempt(bool successful)
        {
            LoginAttempts.WithLabels(successful ? "success" : "failure").Inc();
        }

        public void IncrementUserRegistration()
        {
            UserRegistrations.Inc();
        }

        public void IncrementTokenRefresh()
        {
            TokenRefreshes.Inc();
        }

        public void RecordAuthenticationLatency(double seconds)
        {
            AuthenticationDuration.Observe(seconds);
        }
    }
}
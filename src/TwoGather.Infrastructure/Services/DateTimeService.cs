using TwoGather.Application.Common.Interfaces;

namespace TwoGather.Infrastructure.Services;

public class DateTimeService : IDateTimeService
{
    public DateTime UtcNow => DateTime.UtcNow;
}

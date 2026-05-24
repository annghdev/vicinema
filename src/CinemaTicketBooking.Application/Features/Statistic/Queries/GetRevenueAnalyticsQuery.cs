using CinemaTicketBooking.Application.Abstractions;
using CinemaTicketBooking.Application.Features;

namespace CinemaTicketBooking.Application.Features.Statistic.Queries;

public record GetRevenueAnalyticsQuery(string Period = "monthly") : IQuery<RevenueChartDto>
{
    public string CorrelationId { get; set; } = Guid.NewGuid().ToString();
}

public class GetRevenueAnalyticsHandler(IQueryService queryService)
{
    public async Task<RevenueChartDto> Handle(GetRevenueAnalyticsQuery query, CancellationToken ct)
    {
        string sql;
        var period = query.Period.ToLower();

        if (period == "yearly")
        {
            sql = @"
                SELECT 
                    'T' || EXTRACT(MONTH FROM m) as Label,
                    COALESCE(SUM(b.""FinalAmount""), 0) as Value
                FROM generate_series(
                    date_trunc('year', now()), 
                    date_trunc('year', now()) + interval '11 months', 
                    interval '1 month'
                ) AS m
                LEFT JOIN bookings b ON date_trunc('month', b.""CreatedAt"") = m AND b.""Status"" IN (2, 3)
                GROUP BY m
                ORDER BY m;";
        }
        else if (period == "weekly")
        {
            sql = @"
                SELECT 
                    CASE EXTRACT(DOW FROM d)
                        WHEN 0 THEN 'CN'
                        ELSE 'T' || (EXTRACT(DOW FROM d) + 1)
                    END as Label,
                    COALESCE(SUM(b.""FinalAmount""), 0) as Value
                FROM generate_series(
                    date_trunc('week', now()), 
                    date_trunc('week', now()) + interval '6 days', 
                    interval '1 day'
                ) AS d
                LEFT JOIN bookings b ON date_trunc('day', b.""CreatedAt"") = d AND b.""Status"" IN (2, 3)
                GROUP BY d
                ORDER BY d;";
        }
        else // monthly
        {
            sql = @"
                WITH Weeks AS (
                    SELECT 1 as WeekNum, 'Tuần 1' as Label UNION ALL
                    SELECT 2, 'Tuần 2' UNION ALL
                    SELECT 3, 'Tuần 3' UNION ALL
                    SELECT 4, 'Tuần 4'
                ),
                BookingWeeks AS (
                    SELECT 
                        CASE 
                            WHEN EXTRACT(DAY FROM ""CreatedAt"") BETWEEN 1 AND 7 THEN 1
                            WHEN EXTRACT(DAY FROM ""CreatedAt"") BETWEEN 8 AND 14 THEN 2
                            WHEN EXTRACT(DAY FROM ""CreatedAt"") BETWEEN 15 AND 21 THEN 3
                            ELSE 4
                        END as WeekNum,
                        ""FinalAmount""
                    FROM bookings
                    WHERE ""Status"" IN (2, 3) AND ""CreatedAt"" >= date_trunc('month', now())
                )
                SELECT 
                    w.Label,
                    COALESCE(SUM(bw.""FinalAmount""), 0) as Value
                FROM Weeks w
                LEFT JOIN BookingWeeks bw ON w.WeekNum = bw.WeekNum
                GROUP BY w.WeekNum, w.Label
                ORDER BY w.WeekNum;";
        }

        var results = await queryService.QueryAsync<dynamic>(sql, ct: ct);
        
        var labels = results.Select(x => (string)x.label).ToList();
        var data = results.Select(x => (decimal)x.value).ToList();

        return new RevenueChartDto(labels, data);
    }
}

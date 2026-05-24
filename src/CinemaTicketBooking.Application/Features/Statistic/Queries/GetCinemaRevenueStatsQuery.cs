using CinemaTicketBooking.Application.Abstractions;
using CinemaTicketBooking.Application.Features;

namespace CinemaTicketBooking.Application.Features.Statistic.Queries;

public record GetCinemaRevenueStatsQuery : IQuery<IReadOnlyList<CinemaRevenueDto>>
{
    public string CorrelationId { get; set; } = Guid.NewGuid().ToString();
}

public class GetCinemaRevenueStatsHandler(IQueryService queryService)
{
    public async Task<IReadOnlyList<CinemaRevenueDto>> Handle(GetCinemaRevenueStatsQuery query, CancellationToken ct)
    {
        var sql = @"
            WITH TotalRevenue AS (
                SELECT COALESCE(SUM(""FinalAmount""), 0) as GlobalTotal
                FROM bookings
                WHERE ""Status"" IN (2, 3) AND ""CreatedAt"" >= date_trunc('month', now())
            )
            SELECT 
                c.""Name"" as CinemaName,
                COALESCE(SUM(b.""FinalAmount""), 0) as Revenue,
                CASE 
                    WHEN tr.GlobalTotal > 0 THEN (COALESCE(SUM(b.""FinalAmount""), 0) / tr.GlobalTotal) * 100 
                    ELSE 0 
                END as Percentage
            FROM cinemas c
            LEFT JOIN screens s ON c.""Id"" = s.""CinemaId""
            LEFT JOIN show_times st ON s.""Id"" = st.""ScreenId""
            LEFT JOIN bookings b ON st.""Id"" = b.""ShowTimeId"" AND b.""Status"" IN (2, 3) AND b.""CreatedAt"" >= date_trunc('month', now())
            CROSS JOIN TotalRevenue tr
            GROUP BY c.""Id"", c.""Name"", tr.GlobalTotal
            ORDER BY Revenue DESC;";

        return await queryService.QueryAsync<CinemaRevenueDto>(sql, ct: ct);
    }
}

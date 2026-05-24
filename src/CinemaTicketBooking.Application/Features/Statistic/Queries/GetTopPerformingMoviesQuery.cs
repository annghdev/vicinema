using CinemaTicketBooking.Application.Abstractions;
using CinemaTicketBooking.Application.Features;
using CinemaTicketBooking.Domain;


namespace CinemaTicketBooking.Application.Features.Statistic.Queries;

public record GetTopPerformingMoviesQuery : IQuery<IReadOnlyList<MoviePerformanceDto>>
{
    public string CorrelationId { get; set; } = Guid.NewGuid().ToString();
}

public class GetTopPerformingMoviesHandler(IQueryService queryService)
{
    public async Task<IReadOnlyList<MoviePerformanceDto>> Handle(GetTopPerformingMoviesQuery query, CancellationToken ct)
    {
        var sql = @"
            SELECT 
                m.""Id"" as MovieId,
                m.""Name"" as Title,
                m.""ThumbnailUrl"" as ThumbnailUrl,
                m.""Genre"" as GenreInt,
                m.""CreatedAt"" as ReleaseDate,
                COALESCE(SUM(b.""FinalAmount""), 0) as BoxOffice,
                m.""TargetReach"" as TargetGoal,
                CASE 
                    WHEN m.""TargetReach"" > 0 THEN (COALESCE(SUM(b.""FinalAmount""), 0) / m.""TargetReach"") * 100 
                    ELSE 0 
                END as ProgressPercentage,
                true as IsTrendingUp
            FROM movies m
            LEFT JOIN show_times st ON m.""Id"" = st.""MovieId""
            LEFT JOIN bookings b ON st.""Id"" = b.""ShowTimeId"" AND b.""Status"" IN (2, 3)
            GROUP BY m.""Id"", m.""Name"", m.""ThumbnailUrl"", m.""Genre"", m.""CreatedAt"", m.""TargetReach""
            ORDER BY BoxOffice DESC
            LIMIT 5;";

        var results = await queryService.QueryAsync<dynamic>(sql, ct: ct);
        
        return results.Select(x => new MoviePerformanceDto(
            (Guid)x.movieid,
            (string)x.title,
            (string)x.thumbnailurl,
            ((MovieGenre)x.genreint).ToString(),
            (DateTimeOffset)x.releasedate,
            (decimal)(x.boxoffice ?? 0m),
            (decimal)(x.targetgoal ?? 0m),
            (decimal)(x.progresspercentage ?? 0m),
            (bool)x.istrendingup
        )).ToList();
    }
}

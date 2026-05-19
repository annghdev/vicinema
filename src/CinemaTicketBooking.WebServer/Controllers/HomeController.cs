using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using CinemaTicketBooking.WebServer.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Diagnostics;
using CinemaTicketBooking.Application.Common.Auth;

using CinemaTicketBooking.Application.Features.Statistic.Queries;
using CinemaTicketBooking.Application.Features;
using Wolverine;

namespace CinemaTicketBooking.WebServer.Controllers
{
    [Authorize(AuthenticationSchemes = "Identity.Application")]
    [EnableRateLimiting("fixed")]
public class HomeController(IAuthorizationService authorizationService, IMessageBus bus) : Controller
    {
        public async Task<IActionResult> Index()
        {
            // 1. Full Dashboard access for Management
            if (User.IsInRole(RoleNames.SystemAdmin) || 
                User.IsInRole(RoleNames.Admin) || 
                User.IsInRole(RoleNames.Manager))
            {
                var summaryTask = bus.InvokeAsync<DashboardSummaryDto>(new GetDashboardSummaryQuery());
                var weekChartTask = bus.InvokeAsync<RevenueChartDto>(new GetRevenueAnalyticsQuery("weekly"));
                var monthChartTask = bus.InvokeAsync<RevenueChartDto>(new GetRevenueAnalyticsQuery("monthly"));
                var yearChartTask = bus.InvokeAsync<RevenueChartDto>(new GetRevenueAnalyticsQuery("yearly"));
                var cinemaTask = bus.InvokeAsync<IReadOnlyList<CinemaRevenueDto>>(new GetCinemaRevenueStatsQuery());
                var movieTask = bus.InvokeAsync<IReadOnlyList<MoviePerformanceDto>>(new GetTopPerformingMoviesQuery());

                await Task.WhenAll(summaryTask, weekChartTask, monthChartTask, yearChartTask, cinemaTask, movieTask);

                var model = new DashboardViewModel
                {
                    Summary = await summaryTask,
                    WeeklyRevenueChart = await weekChartTask,
                    MonthlyRevenueChart = await monthChartTask,
                    YearlyRevenueChart = await yearChartTask,
                    CinemaRevenues = await cinemaTask,
                    TopMovies = await movieTask
                };

                return View(model);
            }

            // 2. Specific operational landing zones per Role
            if (User.IsInRole(RoleNames.MovieCoordinator))
            {
                return RedirectToAction("Index", "Movie");
            }

            if (User.IsInRole(RoleNames.TicketStaff))
            {
                return RedirectToAction("Index", "ShowTime"); 
            }

            // Mặc định cho Customer hoặc Role không xác định
            if (User.IsInRole(RoleNames.Customer))
            {
                return RedirectToAction("Logout", "Auth");
            }

            // Absolute fallback (No operations allowed)
            return Forbid(); 
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}


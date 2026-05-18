using CinemaTicketBooking.Domain;
using CinemaTicketBooking.Application.Common.Auth;
using CinemaTicketBooking.Infrastructure.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CinemaTicketBooking.Infrastructure.Persistence;

public class DataSeeder(
    AppDbContext dbContext,
    UserManager<Account> userManager,
    ILogger<DataSeeder> logger)
{
    // =============================================
    // Fixed IDs for deterministic seed data (Simplified format)
    // =============================================
    public static readonly Guid CinemaId1 = new("00000000-0000-0000-0000-000000000001");
    public static readonly Guid CinemaId2 = new("00000000-0000-0000-0000-000000000002");

    public static readonly Guid MovieId1 = new("00000000-0000-0000-0000-000000010001");
    public static readonly Guid MovieId2 = new("00000000-0000-0000-0000-000000010002");
    public static readonly Guid MovieId3 = new("00000000-0000-0000-0000-000000010003");

    public static readonly Guid ConcessionId1 = new("00000000-0000-0000-0000-000000020001");
    public static readonly Guid ConcessionId2 = new("00000000-0000-0000-0000-000000020002");
    public static readonly Guid ConcessionId3 = new("00000000-0000-0000-0000-000000020003");

    public static readonly Guid ScreenId1 = new("00000000-0000-0000-0000-000000030001");
    public static readonly Guid ScreenId2 = new("00000000-0000-0000-0000-000000030002");
    public static readonly Guid ScreenId3 = new("00000000-0000-0000-0000-000000030003");
    public static readonly Guid ScreenId4 = new("00000000-0000-0000-0000-000000030004");
    public static readonly Guid ScreenId5 = new("00000000-0000-0000-0000-000000030005");

    public static readonly Guid ShowTimeId1 = new("00000000-0000-0000-0000-000000040001");
    public static readonly Guid ShowTimeId2 = new("00000000-0000-0000-0000-000000040002");
    public static readonly Guid ShowTimeId3 = new("00000000-0000-0000-0000-000000040003");
    public static readonly Guid ShowTimeId4= new("00000000-0000-0000-0000-000000040004");
    public static readonly Guid ShowTimeId5= new("00000000-0000-0000-0000-000000040005");


    public static readonly Guid TicketId1 = new("00000000-0000-0000-0000-000000050001");

    public static readonly Guid CustomerId1 = new("00000000-0000-0000-0000-000000060001");
    public static readonly Guid CustomerId2 = new("00000000-0000-0000-0000-000000060002");
    public static readonly Guid CustomerId3 = new("00000000-0000-0000-0000-000000060003");
    public static readonly Guid CustomerId4 = new("00000000-0000-0000-0000-000000060004");

    public static readonly Guid BookingId1 = new("00000000-0000-0000-0000-000000070001");
    public static readonly Guid BookingId2 = new("00000000-0000-0000-0000-000000070002");
    public static readonly Guid BookingId3 = new("00000000-0000-0000-0000-000000070003");
    public static readonly Guid BookingId4 = new("00000000-0000-0000-0000-000000070004");
    public static readonly Guid BookingId5 = new("00000000-0000-0000-0000-000000070005");


    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        // 1. Seed root catalogs if they don't exist.
        if (!await dbContext.Cinemas.AnyAsync(cancellationToken))
        {
            var cinemas = SeedCinemas();
            var movies = SeedMovies();
            var concessions = SeedConcessions();
            var seatSelectionPolicy = SeatSelectionPolicy.CreateDefault();
            var pricingPolicies = SeedPricingPolicies();
            
            var screens = SeedScreens(cinemas);
            var showTimes = SeedShowTimes(movies, screens, pricingPolicies);
            var customers = SeedCustomers();
            var bookings = SeedBookings(showTimes, customers, concessions);

            dbContext.Cinemas.AddRange(cinemas);
            dbContext.Movies.AddRange(movies);
            dbContext.Concessions.AddRange(concessions);
            dbContext.SeatSelectionPolicies.Add(seatSelectionPolicy);
            dbContext.PricingPolicies.AddRange(pricingPolicies);
            dbContext.Screens.AddRange(screens);
            dbContext.ShowTimes.AddRange(showTimes);
            dbContext.Customers.AddRange(customers);
            dbContext.Bookings.AddRange(bookings);
            
            await dbContext.SaveChangesAsync(cancellationToken);
            await SeedRegisteredAccountsAsync(customers, cancellationToken);
            logger.LogInformation("Seeded core demo data successfully.");
        }

        // 2. Ensure Slides exist.
        if (!await dbContext.Slides.AnyAsync(cancellationToken))
        {
            var slides = SeedSlides();
            dbContext.Slides.AddRange(slides);
            await dbContext.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Seeded slide data successfully.");
        }
    }

    private static List<Cinema> SeedCinemas()
    {
        var cinema1 = Cinema.Create(
            name: "Vicinema Nguyễn Du",
            thumbnailUrl: "https://images.unsplash.com/photo-1489599849927-2ee91cede3ba",
            geo: "10.773300,106.699700",
            address: "116 Nguyen Du, District 1, TP.HCM",
            isActive: true);
        cinema1.Id = CinemaId1;

        var cinema2 = Cinema.Create(
            name: "Vicinema Trần Quang Khải",
            thumbnailUrl: "https://images.unsplash.com/photo-1517602302552-471fe67acf66",
            geo: "10.790400,106.692700",
            address: "204 Tran Quang Khai, District 1, TP.HCM",
            isActive: true);
        cinema2.Id = CinemaId2;

        var cinema3 = Cinema.Create(
            name: "Vicinem Sư Vạn Hạnh",
            thumbnailUrl: "https://images.unsplash.com/photo-1595769816263-9b910be24d5f",
            geo: "10.828800,106.683400",
            address: "242 Sư Vạn Hạnh, Quận 10, TP.HCM",
            isActive: true);
        cinema3.Id = new Guid("00000000-0000-0000-0000-000000000003");

        var cinema4 = Cinema.Create(
            name: "Vicinema Vincom Landmark 81",
            thumbnailUrl: "https://images.unsplash.com/photo-1517602302552-471fe67acf66",
            geo: "10.794600,106.721400",
            address: "720A Dien Bien Phu, P.22, Quận Bình Thạnh, TP.HCM",
            isActive: true);
        cinema4.Id = new Guid("00000000-0000-0000-0000-000000000004");

        var cinema5 = Cinema.Create(
            name: "Viciema Thảo Điền",
            thumbnailUrl: "https://images.unsplash.com/photo-1595769816263-9b910be24d5f",
            geo: "10.803700,106.736000",
            address: "159 QL1A, Thao Dien, TP.Thủ Đức",
            isActive: true);
        cinema5.Id = new Guid("00000000-0000-0000-0000-000000000005");

        return [cinema1, cinema2, cinema3, cinema4, cinema5];
    }

    private static List<Slide> SeedSlides()
    {
        return [
            Slide.Create("MORTAL KOMBAT II", "Cuộc chiến sinh tử tiếp tục bùng nổ với những võ sĩ huyền thoại.", "https://images.unsplash.com/photo-1542204165-65bf26472b9b", "/movies", 1, SlideType.ShowingMovie, "https://www.youtube.com/watch?v=TcMBFSGVi1c"),
            Slide.Create("DORAEMON MOVIE 45", "Tân Nobita và lâu đài dưới đáy biển - Hành trình khám phá đại dương kỳ ảo.", "https://images.unsplash.com/photo-1518709268805-4e9042af9f23", "/movies", 2, SlideType.UpcomingMovie),
            Slide.Create("MANDALORIAN & GROGU", "Sự trở lại của thợ săn tiền thưởng và cậu bé Grogu trên màn ảnh rộng.", "https://images.unsplash.com/photo-1440404653325-ab127d49abc1", "/movies", 3, SlideType.UpcomingMovie),
            Slide.Create("LỄ HỘI PHIM:CIBEF 2026", "Tham gia ngay lễ hội phim quốc tế lớn nhất năm tại Absolute Cinema.", "https://images.unsplash.com/photo-1513558161293-cdaf765ed2fd", "/promos/cibef-2026", 4, SlideType.Event),
            Slide.Create("SHIN - CẬU BÉ BÚT CHÌ", "Quậy tung Vương quốc Nguệch ngoạc cùng 4 dũng sĩ bất ổn.", "https://images.unsplash.com/photo-1536440136628-849c177e76a1", "/movies", 5, SlideType.ShowingMovie, "https://www.youtube.com/watch?v=JfVOs4VSpmA"),
            Slide.Create("YÊU NỮ THÍCH HÀNG HIỆU 2", "Sự trở lại của bà trùm thời trang Miranda Priestly.", "https://images.unsplash.com/photo-1509281373149-e957c6296406", "/movies", 6, SlideType.ShowingMovie),
            Slide.Create("GẤU BOONIE:KUNGFU ẨN SĨ", "Hành trình tầm sư học đạo đầy hài hước của anh em nhà gấu.", "https://images.unsplash.com/photo-1585647347483-22b66260dfff", "/movies", 7, SlideType.ShowingMovie),
            Slide.Create("THẨM MỸ VIỆN ÂM PHỦ", "Bí mật kinh hoàng đằng sau những ca phẫu thuật thay đổi cuộc đời.", "https://images.unsplash.com/photo-1489599849927-2ee91cede3ba", "/movies", 8, SlideType.ShowingMovie, "https://www.youtube.com/watch?v=uYPbbksJxIg"),
            Slide.Create("PHI VỤ THANH TOÁN", "Cuộc chiến khốc liệt trong giới thượng lưu để giành lấy quyền lực.", "https://images.unsplash.com/photo-1517602302552-471fe67acf66", "/movies", 9, SlideType.ShowingMovie),
            Slide.Create("SLIME:NƯỚC MẮT ĐẠI DƯƠNG", "Rimuru và những người bạn trong cuộc phiêu lưu mới tại vương quốc biển.", "https://images.unsplash.com/photo-1626814026160-2237a95fc5a0", "/movies", 10, SlideType.ShowingMovie),
        ];
    }

    private static List<Movie> SeedMovies()
    {
        var movies = new List<Movie>();

        // --- 10 Now Showing - Real Data Vietnam May 2026 ---
        
        // 1. Shin - Cậu Bé Bút Chì
        var m1 = Movie.Create("Shin – Cậu Bé Bút Chì: Quậy Tung Vương Quốc Nguệch Ngoạc", "Cậu bé Shin vô tình nắm giữ cây bút chì màu kỳ diệu để bảo vệ vương quốc Rakuga.", "https://images.unsplash.com/photo-1536440136628-849c177e76a1", "Toho", "Masakazu Hashimoto", "https://www.youtube.com/watch?v=TcMBFSGVi1c", 104, MovieGenre.Animation, MovieStatus.NowShowing, 15000000m);
        m1.Id = MovieId1;
        movies.Add(m1);

        // 2. Mortal Kombat II
        var m2 = Movie.Create("Mortal Kombat II: Cuộc Chiến Sinh Tử", "Cuộc chiến khốc liệt giữa phe Địa Giới và kẻ ác bước vào giai đoạn quyết định.", "https://images.unsplash.com/photo-1542204165-65bf26472b9b", "Warner Bros.", "Simon McQuoid", "https://www.youtube.com/watch?v=pBk4NYhWNMM", 125, MovieGenre.Action, MovieStatus.NowShowing, 25000000m);
        m2.Id = MovieId2;
        movies.Add(m2);

        // 3. Thẩm Mỹ Viện Âm Phủ
        movies.Add(Movie.Create("Thẩm Mỹ Viện Âm Phủ", "Câu chuyện kinh dị về Thanh rơi vào vòng xoáy của các nghi lễ ma quái tại một thẩm mỹ viện hẻo lánh.", "https://images.unsplash.com/photo-1489599849927-2ee91cede3ba", "VN Films", "Đỗ Đức Thịnh", "https://www.youtube.com/watch?v=uYPbbksJxIg", 110, MovieGenre.Horror, MovieStatus.NowShowing, 12000000m));

        // 4. Yêu Nữ Thích Hàng Hiệu 2
        movies.Add(Movie.Create("Yêu Nữ Thích Hàng Hiệu 2", "Sự trở lại của bà trùm thời trang Miranda Priestly và cuộc đối đầu với người trợ lý cũ.", "https://images.unsplash.com/photo-1509281373149-e957c6296406", "Disney", "David Frankel", "https://www.youtube.com/watch?v=JfVOs4VSpmA", 115, MovieGenre.Comedy, MovieStatus.NowShowing, 180000000m));

        // 5. Gấu Boonie
        movies.Add(Movie.Create("Gấu Boonie: Kungfu Ẩn Sĩ", "Hành trình tầm sư học đạo đầy hài hước của anh em nhà gấu.", "https://images.unsplash.com/photo-1585647347483-22b66260dfff", "Fantawild", "Huida Lin", "https://www.youtube.com/watch?v=JfVOs4VSpmA", 95, MovieGenre.Animation, MovieStatus.NowShowing, 900000000m));

        // 6. Phi Vụ Thanh Toán
        movies.Add(Movie.Create("Phi Vụ Thanh Toán", "Một vụ giết người bí ẩn kéo theo những âm mưu thâm độc trong giới thượng lưu.", "https://images.unsplash.com/photo-1517602302552-471fe67acf66", "CJ ENM", "Park Sang-hyun", null, 118, MovieGenre.Thriller, MovieStatus.NowShowing, 110000000m));

        // 7. Slime: Nước Mắt Đại Dương
        movies.Add(Movie.Create("Lúc Đó Tôi Đã Chuyển Sinh Thành Slime: Nước Mắt Đại Dương", "Rimuru bắt đầu chuyến phiêu lưu mới tại vương quốc biển xa xôi.", "https://images.unsplash.com/photo-1626814026160-2237a95fc5a0", "Bandai Namco", "Yasuhito Kikuchi", null, 108, MovieGenre.Animation, MovieStatus.NowShowing, 130000000m));

        // 8. Vây Hãm: Kẻ Trừng Phạt
        movies.Add(Movie.Create("Vây Hãm: Kẻ Trừng Phạt", "Thanh tra Ma Seok-do đối đầu với một tổ chức tội phạm công nghệ quy mô lớn.", "https://images.unsplash.com/photo-1536440136628-849c177e76a1", "ABO Entertainment", "Heo Myung-haeng", null, 109, MovieGenre.Action, MovieStatus.NowShowing, 200000000m));

        // 9. Phổi Sắt
        movies.Add(Movie.Create("Phổi Sắt", "Một thủy thủ bị nhốt trong tàu ngầm mini khám phá đại dương máu trên hành tinh xa lạ.", "https://images.unsplash.com/photo-1536440136628-849c177e76a1", "Markiplier", "Mark Fischbach", null, 85, MovieGenre.Horror, MovieStatus.NowShowing, 7000000000m));

        // 10. Đội Thám Tử Cừu
        movies.Add(Movie.Create("Đội Thám Tử Cừu: Án Mạng Lúc Nửa Đêm", "Vụ án mạng bí ẩn trong trang trại đòi hỏi sự thông minh của biệt đội thám tử cừu.", "https://images.unsplash.com/photo-1513558161293-cdaf765ed2fd", "Studio Canal", "Richard Starzak", null, 92, MovieGenre.Animation, MovieStatus.NowShowing, 6000000000m));


        // --- 6 Upcoming - Real Data Vietnam May 2026 ---

        // 1. Doraemon Movie 45
        var u1 = Movie.Create("Doraemon Movie 45: Tân Nobita Và Lâu Đài Dưới Đáy Biển", "Chuyến phiêu lưu mới của nhóm bạn Doraemon tại thế giới bí ẩn dưới đáy biển.", "https://images.unsplash.com/photo-1518709268805-4e9042af9f23", "Shin-Ei Animation", "Susumu Mitsunaka", null, 105, MovieGenre.Animation, MovieStatus.Upcoming, 2000000m);
        u1.Id = MovieId3;
        movies.Add(u1);

        // 2. Mandalorian & Grogu
        movies.Add(Movie.Create("The Mandalorian and Grogu", "Hành trình mới của Mando và Grogu sau các sự kiện trong series.", "https://images.unsplash.com/photo-1440404653325-ab127d49abc1", "Lucasfilm", "Jon Favreau", null, 130, MovieGenre.SciFi, MovieStatus.Upcoming, 30000000m));

        // 3. Ma Da Hàn Quốc
        movies.Add(Movie.Create("Ma Da Hàn Quốc: Hồ Nuốt Người", "Truyền thuyết kinh dị về linh hồn dưới hồ nước đang chờ đợi kẻ xấu số tiếp theo.", "https://images.unsplash.com/photo-1585647347483-22b66260dfff", "Showbox", "Jang Jae-hyun", null, 122, MovieGenre.Horror, MovieStatus.Upcoming, 150000000m));

        // 4. Mother Mary
        movies.Add(Movie.Create("Mother Mary: Hào Quang Đơn Độc", "Mối quan hệ phức tạp giữa một ngôi sao nhạc Pop và nhà thiết kế thời trang tài năng.", "https://images.unsplash.com/photo-1626814026160-2237a95fc5a0", "A24", "David Lowery", null, 112, MovieGenre.Drama, MovieStatus.Upcoming, 9000000000m));

        // 5. Một Thời Ta Đã Yêu
        movies.Add(Movie.Create("Một Thời Ta Đã Yêu", "Câu chuyện tình lãng mạn đầy nuối tiếc của hai người trẻ giữa Sài Gòn hoa lệ.", "https://images.unsplash.com/photo-1517602302552-471fe67acf66", "Skyline", "Nguyễn Phan Quang Bình", null, 106, MovieGenre.Romance, MovieStatus.Upcoming, 8000000000m));

        // 6. KHÁCH
        movies.Add(Movie.Create("KHÁCH", "Vị khách không mời mang theo những bí mật chết người đến ngôi biệt thự hẻo lánh.", "https://images.unsplash.com/photo-1509281373149-e957c6296406", "A24", "Ti West", null, 102, MovieGenre.Thriller, MovieStatus.Upcoming, 7000000000m));


        // --- 4 Classic / No Show ---
        for (int i = 1; i <= 4; i++)
        {
            movies.Add(Movie.Create($"Phim Kinh Điển {i}", $"Tác phẩm điện ảnh kinh điển {i} đã từng nhận được nhiều giải thưởng lớn.", "https://images.unsplash.com/photo-1542204165-65bf26472b9b", "Heritage Films", "K. Master", null, 120 + i, MovieGenre.Drama, MovieStatus.NoShow, 1000000000m * i));
        }

        return movies;
    }

    private static List<Concession> SeedConcessions()
    {
        var concession1 = Concession.Create(
            name: "Combo Popcorn + Cola",
            price: 99000m,
            imageUrl: "https://images.unsplash.com/photo-1585647347483-22b66260dfff",
            isAvailable: true);
        concession1.Id = ConcessionId1;

        var concession2 = Concession.Create(
            name: "Caramel Popcorn",
            price: 69000m,
            imageUrl: "https://images.unsplash.com/photo-1578849278619-e73505e9610f",
            isAvailable: true);
        concession2.Id = ConcessionId2;

        var concession3 = Concession.Create(
            name: "Sparkling Orange",
            price: 39000m,
            imageUrl: "https://images.unsplash.com/photo-1513558161293-cdaf765ed2fd",
            isAvailable: true);
        concession3.Id = ConcessionId3;

        return [concession1, concession2, concession3];
    }

    private static List<PricingPolicy> SeedPricingPolicies()
    {
        return
        [
            // TwoD - Weekend surcharge 20%
            PricingPolicy.Create(null, ScreenType.TwoD, SeatType.Regular, 85000m, 1.00m, 1.20m, true),
            PricingPolicy.Create(null, ScreenType.TwoD, SeatType.VIP, 120000m, 1.00m, 1.20m, true),
            PricingPolicy.Create(null, ScreenType.TwoD, SeatType.Sweetbox, 170000m, 1.00m, 1.20m, true),

            // ThreeD - Weekend surcharge 15%
            PricingPolicy.Create(null, ScreenType.ThreeD, SeatType.Regular, 85000m, 1.25m, 1.15m, true),
            PricingPolicy.Create(null, ScreenType.ThreeD, SeatType.VIP, 120000m, 1.25m, 1.15m, true),
            PricingPolicy.Create(null, ScreenType.ThreeD, SeatType.Sweetbox, 170000m, 1.25m, 1.15m, true),

            // IMAX - Weekend surcharge 10%
            PricingPolicy.Create(null, ScreenType.IMAX, SeatType.Regular, 85000m, 1.55m, 1.10m, true),
            PricingPolicy.Create(null, ScreenType.IMAX, SeatType.VIP, 120000m, 1.55m, 1.10m, true),
            PricingPolicy.Create(null, ScreenType.IMAX, SeatType.Sweetbox, 170000m, 1.55m, 1.10m, true)
        ];
    }

    private static List<Screen> SeedScreens(List<Cinema> cinemas)
    {
        // SeatMap encoding: 0=aisle, 1=Regular, 2=VIP, 3=SweetBox, 4=SweetBox couple gap spacer.
        // Row 5 of standard: Sweet3 | aisle | aisle | Sweet2 | [gap] | Sweet1 | aisle | aisle
        var standardSeatMap = "[[1,1,0,2,2,2,1,0],[1,1,0,2,2,2,1,0],[1,1,0,2,2,2,1,0],[1,1,0,2,2,2,1,0],[4,3,0,4,3,4,3,0]]";
        // Row 4 of compact: Sweet3 | Sweet2 | Sweet1 | aisle
        var compactSeatMap = "[[1,1,0,2,2,1,0],[1,1,0,2,2,1,0],[1,1,0,2,2,1,0],[4,3,0,3,4,3,0]]";

        var mediumSeatMap =
         "[[0,1,1,1,1,1,1,1,1,1,1,0],"
         +"[0,1,1,2,2,2,2,2,2,1,1,0],"
         +"[0,1,1,2,2,2,2,2,2,1,1,0],"
         +"[0,1,1,2,2,2,2,2,2,1,1,0],"
         +"[0,4,3,4,3,4,3,4,3,4,3,0]]";

        var largestSeatMap =
            "[[1,1,1,1,0,1,1,1,1,1,1,1,1,0,1,1,1,1],"
            + "[1,1,1,1,0,1,1,1,1,1,1,1,1,0,1,1,1,1],"
            + "[1,1,1,1,0,2,2,2,2,2,2,2,2,0,1,1,1,1],"
            + "[1,1,1,1,0,2,2,2,2,2,2,2,2,0,1,1,1,1],"
            + "[1,1,1,1,0,2,2,2,2,2,2,2,2,0,1,1,1,1],"
            + "[1,1,1,1,0,2,2,2,2,2,2,2,2,0,1,1,1,1],"
            + "[1,1,1,1,0,2,2,2,2,2,2,2,2,0,1,1,1,1],"
            + "[1,1,1,1,0,4,3,4,3,4,3,4,3,0,1,1,1,1],"
            + "[4,3,4,3,0,4,3,4,3,4,3,4,3,0,4,3,4,3]]";


        var screenList = new List<Screen>();
        int screenIndex = 1;

        foreach (var cinema in cinemas)
        {
            var initials = string.Concat(cinema.Name.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(s => s[0])).ToUpper();
            int screenNum = 1;

            var s1 = CreateScreen(cinema.Id, $"{initials}-S{screenNum++}", standardSeatMap, ScreenType.TwoD, [ScreenType.TwoD]);
            var s2 = CreateScreen(cinema.Id, $"{initials}-S{screenNum++}", compactSeatMap, ScreenType.ThreeD, [ScreenType.TwoD, ScreenType.ThreeD]);
            var s3 = CreateScreen(cinema.Id, $"{initials}-S{screenNum++}", standardSeatMap, ScreenType.IMAX, [ScreenType.TwoD, ScreenType.ThreeD, ScreenType.IMAX]);
            var s4 = CreateScreen(cinema.Id, $"{initials}-S{screenNum++}", mediumSeatMap, ScreenType.IMAX, [ScreenType.TwoD, ScreenType.ThreeD, ScreenType.IMAX]);
            var s5 = CreateScreen(cinema.Id, $"{initials}-S{screenNum++}", largestSeatMap, ScreenType.IMAX, [ScreenType.TwoD, ScreenType.ThreeD, ScreenType.IMAX]);

            // Assign fixed IDs only to the first cinema's screens to preserve existing tests/seed references
            if (cinema.Id == CinemaId1)
            {
                s1.Id = ScreenId1;
                s2.Id = ScreenId2;
                s3.Id = ScreenId3;
                s4.Id = ScreenId4;
                s5.Id = ScreenId5;
            }

            screenList.AddRange([s1, s2, s3, s4, s5]);
        }

        return screenList;
    }

    private static Screen CreateScreen(Guid cinemaId, string code, string seatMap, ScreenType type, List<ScreenType> supportedFormats)
    {
        var rowCount = seatMap.Count(c => c == '[') - 1;
        var firstRowStart = seatMap.IndexOf("[", StringComparison.Ordinal) + 1;
        var firstRowEnd = seatMap.IndexOf("]", StringComparison.Ordinal);
        var firstRow = seatMap[firstRowStart..firstRowEnd];
        var columnCount = firstRow.Split(',', StringSplitOptions.RemoveEmptyEntries).Length;
        var totalSeats = seatMap.Count(c => c is '1' or '2' or '3');

        var screen = Screen.Create(
            cinemaId: cinemaId,
            code: code,
            rowOfSeats: rowCount,
            columnOfSeats: columnCount,
            totalSeats: totalSeats,
            seatMap: seatMap,
            type: type,
            isActive: true,
            supportedFormats: supportedFormats);

        screen.GenerateSeats(seatMap);
        return screen;
    }

    private static List<ShowTime> SeedShowTimes(
        List<Movie> movies,
        List<Screen> screens,
        List<PricingPolicy> pricingPolicies)
    {
        var showTimes = new List<ShowTime>();
        var now = DateTimeOffset.UtcNow;
        var nowShowingMovies = movies.Where(m => m.Status == MovieStatus.NowShowing).ToList();
        
        int showtimeIndex = 0;
        var fixedIds = new[] { ShowTimeId1, ShowTimeId2, ShowTimeId3, ShowTimeId4, ShowTimeId5 };

        // Create showtimes for the next 3 days
        for (int day = 0; day < 3; day++)
        {
            var date = now.Date.AddDays(day);
            
            // For each Cinema
            for (int cinemaIdx = 0; cinemaIdx < 5; cinemaIdx++)
            {
                var cinemaScreens = screens.Skip(cinemaIdx * 5).Take(5).ToList();
                
                for (int screenIdx = 0; screenIdx < cinemaScreens.Count; screenIdx++)
                {
                    var screen = cinemaScreens[screenIdx];
                    var lastEndTime = DateTimeOffset.MinValue;
                    
                    // 2 showtimes per screen per day
                    for (int session = 0; session < 2; session++)
                    {
                        var movie = nowShowingMovies[showtimeIndex % nowShowingMovies.Count];
                        
                        var vnOffset = TimeSpan.FromHours(7);
                        var vnDate = now.ToOffset(vnOffset).Date.AddDays(day);
                        
                        double hourOffset = (session == 0 ? 9.0 : 18.0) + (screenIdx * 0.5) + (cinemaIdx * 0.5);
                        var startAt = new DateTimeOffset(vnDate, vnOffset).AddHours(hourOffset).ToUniversalTime();

                        if (startAt <= now)
                        {
                            startAt = now.AddHours(2 + session * 3);
                        }

                        // Prevent overlap
                        if (startAt < lastEndTime.AddMinutes(30))
                        {
                            startAt = lastEndTime.AddMinutes(30);
                        }

                        // Ensure not between 2 AM and 6 AM VN
                        var startVn = startAt.ToOffset(vnOffset);
                        if (startVn.Hour >= 2 && startVn.Hour < 6)
                        {
                            // push to 6 AM VN
                            startAt = new DateTimeOffset(startVn.Year, startVn.Month, startVn.Day, 6, 0, 0, vnOffset).ToUniversalTime();
                        }
                        
                        lastEndTime = startAt.AddMinutes(movie.Duration);

                        // Pick format from supported formats
                        var format = screen.SupportedFormats[showtimeIndex % screen.SupportedFormats.Count];
                        
                        var st = ShowTime.Create(
                            movie: movie,
                            screen: screen,
                            startAt: startAt,
                            pricingPolicies: pricingPolicies.Where(p => p.ScreenType == format).ToList(),
                            format: format);
                        
                        if (showtimeIndex < fixedIds.Length)
                        {
                            st.Id = fixedIds[showtimeIndex];
                            foreach (var ticket in st.Tickets)
                            {
                                ticket.ShowTimeId = fixedIds[showtimeIndex];
                            }
                        }

                        if (showtimeIndex == 0 && st.Tickets.Count > 0)
                        {
                            st.Tickets.Last().Id = TicketId1;
                        }

                        showTimes.Add(st);
                        showtimeIndex++;
                    }
                }
            }
        }

        return showTimes;
    }

    private static List<Customer> SeedCustomers()
    {
        var customer1 = Customer.Create(
            name: "Nguyen Minh Anh",
            sessionId: "seed-session-registered-01",
            phoneNumber: "0901234567",
            email: "minh.anh@example.com",
            isRegistered: true);
        customer1.Id = CustomerId1;

        var customer2 = Customer.Create(
            name: "Tran Gia Bao",
            sessionId: "seed-session-guest-01",
            phoneNumber: "0912345678",
            email: "gia.bao@example.com",
            isRegistered: false);
        customer2.Id = CustomerId2;

        var customer3 = Customer.Create(
            name: "Le Thanh Vu",
            sessionId: "seed-session-registered-02",
            phoneNumber: "0923456789",
            email: "thanh.vu@example.com",
            isRegistered: true);
        customer3.Id = CustomerId3;

        var customer4 = Customer.Create(
            name: "Pham Quynh Nhu",
            sessionId: "seed-session-guest-02",
            phoneNumber: "0934567890",
            email: "quynh.nhu@example.com",
            isRegistered: false);
        customer4.Id = CustomerId4;

        return [customer1, customer2, customer3, customer4];
    }

    private static List<Booking> SeedBookings(
        List<ShowTime> showTimes,
        List<Customer> customers,
        List<Concession> concessions)
    {
        var bookings = new List<Booking>();

        // 1. Confirmed booking (registered customer, with concessions)
        var bookingConfirmed = CreatePendingBooking(showTimes[0], customers[0]);
        bookingConfirmed.Id = BookingId1;
        AttachTicketsForCheckout(bookingConfirmed, showTimes[0], customers[0], ticketCount: 2, ticketOffset: 0);
        bookingConfirmed.AddConcession(concessions[0], quantity: 1);
        bookingConfirmed.AddConcession(concessions[2], quantity: 2);
        bookingConfirmed.UpdateFinalAmount(discountAmount: 10000m);
        bookingConfirmed.Confirm();
        bookings.Add(bookingConfirmed);

        // 2. Pending booking (guest customer, waiting for payment)
        var bookingPending = CreatePendingBooking(showTimes[1], customers[1]);
        bookingPending.Id = BookingId2;
        AttachTicketsForCheckout(bookingPending, showTimes[1], customers[1], ticketCount: 3, ticketOffset: 0);
        bookingPending.AddConcession(concessions[1], quantity: 1);
        bookingPending.UpdateFinalAmount(discountAmount: 0m);
        bookings.Add(bookingPending);

        // 3. Checked-in booking (registered customer)
        var bookingCheckedIn = CreatePendingBooking(showTimes[2], customers[2]);
        bookingCheckedIn.Id = BookingId3;
        AttachTicketsForCheckout(bookingCheckedIn, showTimes[2], customers[2], ticketCount: 2, ticketOffset: 0);
        bookingCheckedIn.AddConcession(concessions[0], quantity: 1);
        bookingCheckedIn.UpdateFinalAmount(discountAmount: 15000m);
        bookingCheckedIn.Confirm();
        bookingCheckedIn.CheckIn();
        bookings.Add(bookingCheckedIn);

        // 4. Cancelled booking (guest customer, released tickets)
        var bookingCancelled = CreatePendingBooking(showTimes[0], customers[3]);
        bookingCancelled.Id = BookingId4;
        AttachTicketsForCheckout(bookingCancelled, showTimes[0], customers[3], ticketCount: 2, ticketOffset: 3);
        bookingCancelled.AddConcession(concessions[2], quantity: 1);
        bookingCancelled.UpdateFinalAmount(discountAmount: 5000m);
        bookingCancelled.Cancel();
        bookings.Add(bookingCancelled);

        // 5. Another confirmed booking to enrich dashboard/API data
        var bookingConfirmed2 = CreatePendingBooking(showTimes[1], customers[0]);
        bookingConfirmed2.Id = BookingId5;
        AttachTicketsForCheckout(bookingConfirmed2, showTimes[1], customers[0], ticketCount: 1, ticketOffset: 4);
        bookingConfirmed2.AddConcession(concessions[1], quantity: 2);
        bookingConfirmed2.UpdateFinalAmount(discountAmount: 0m);
        bookingConfirmed2.Confirm();
        bookings.Add(bookingConfirmed2);

        return bookings;
    }

    private static Booking CreatePendingBooking(ShowTime showTime, Customer customer)
    {
        var booking = Booking.Create(
            showTimeId: showTime.Id,
            customerId: customer.Id,
            customerName: customer.Name,
            phoneNumber: customer.PhoneNumber,
            email: customer.Email,
            status: BookingStatus.Pending);

        booking.Customer = customer;
        booking.ShowTime = showTime;
        return booking;
    }

    private static void AttachTicketsForCheckout(
        Booking booking,
        ShowTime showTime,
        Customer customer,
        int ticketCount,
        int ticketOffset)
    {
        var lockUntil = DateTimeOffset.UtcNow.AddMinutes(10);
        var paymentUntil = DateTimeOffset.UtcNow.AddMinutes(20);
        var selectedTickets = showTime.Tickets.Skip(ticketOffset).Take(ticketCount).ToList();

        foreach (var ticket in selectedTickets)
        {
            ticket.Lock(customer.SessionId, lockUntil);
            booking.AddTicket(ticket);
            ticket.StartPayment(booking.Id, customer.SessionId, paymentUntil);
        }
    }

    private async Task SeedRegisteredAccountsAsync(
        List<Customer> customers,
        CancellationToken cancellationToken)
    {
        var registeredCustomers = customers.Where(x => x.IsRegistered).ToList();
        foreach (var customer in registeredCustomers)
        {
            var existing = await userManager.FindByEmailAsync(customer.Email);
            if (existing is not null)
            {
                continue;
            }

            var account = new Account
            {
                Id = Guid.CreateVersion7(),
                UserName = customer.Email,
                Email = customer.Email,
                EmailConfirmed = true,
                PhoneNumber = customer.PhoneNumber,
                CustomerId = customer.Id
            };

            var password = "SeedDemo@123";
            var created = await userManager.CreateAsync(account, password);
            if (!created.Succeeded)
            {
                logger.LogWarning(
                    "Failed to create seeded account for {Email}: {Errors}",
                    customer.Email,
                    string.Join(", ", created.Errors.Select(e => e.Description)));
                continue;
            }

            var roleResult = await userManager.AddToRoleAsync(account, RoleNames.Customer);
            if (!roleResult.Succeeded)
            {
                logger.LogWarning(
                    "Failed to add seeded account {Email} to role Customer: {Errors}",
                    customer.Email,
                    string.Join(", ", roleResult.Errors.Select(e => e.Description)));
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

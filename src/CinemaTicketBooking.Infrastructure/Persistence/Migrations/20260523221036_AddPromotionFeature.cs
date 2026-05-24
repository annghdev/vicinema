using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CinemaTicketBooking.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPromotionFeature : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsFree",
                table: "booking_concessions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "booking_promotions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BookingId = table.Column<Guid>(type: "uuid", nullable: false),
                    PromotionProgramId = table.Column<Guid>(type: "uuid", nullable: false),
                    PromotionName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    DiscountType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    BookingId1 = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_booking_promotions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_booking_promotions_bookings_BookingId",
                        column: x => x.BookingId,
                        principalTable: "bookings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_booking_promotions_bookings_BookingId1",
                        column: x => x.BookingId1,
                        principalTable: "bookings",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "customer_promotion_usages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    PromotionProgramId = table.Column<Guid>(type: "uuid", nullable: false),
                    BookingId = table.Column<Guid>(type: "uuid", nullable: false),
                    UsedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_customer_promotion_usages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "promotion_programs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    PosterImage = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    StartDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EndDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    DiscountType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    DiscountForm = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    DiscountValue = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    MaxDiscountAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    MaxDiscountPercentage = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    MaxUsagePerCustomer = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_promotion_programs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "promotion_conditions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PromotionProgramId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConditionType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Value = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    SecondaryValue = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_promotion_conditions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_promotion_conditions_promotion_programs_PromotionProgramId",
                        column: x => x.PromotionProgramId,
                        principalTable: "promotion_programs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "promotion_free_concession_items",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PromotionProgramId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConcessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_promotion_free_concession_items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_promotion_free_concession_items_promotion_programs_Promotio~",
                        column: x => x.PromotionProgramId,
                        principalTable: "promotion_programs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_booking_promotions_BookingId",
                table: "booking_promotions",
                column: "BookingId");

            migrationBuilder.CreateIndex(
                name: "IX_booking_promotions_BookingId1",
                table: "booking_promotions",
                column: "BookingId1");

            migrationBuilder.CreateIndex(
                name: "IX_customer_promotion_usages_CustomerId_PromotionProgramId",
                table: "customer_promotion_usages",
                columns: new[] { "CustomerId", "PromotionProgramId" });

            migrationBuilder.CreateIndex(
                name: "IX_promotion_conditions_PromotionProgramId",
                table: "promotion_conditions",
                column: "PromotionProgramId");

            migrationBuilder.CreateIndex(
                name: "IX_promotion_free_concession_items_PromotionProgramId",
                table: "promotion_free_concession_items",
                column: "PromotionProgramId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "booking_promotions");

            migrationBuilder.DropTable(
                name: "customer_promotion_usages");

            migrationBuilder.DropTable(
                name: "promotion_conditions");

            migrationBuilder.DropTable(
                name: "promotion_free_concession_items");

            migrationBuilder.DropTable(
                name: "promotion_programs");

            migrationBuilder.DropColumn(
                name: "IsFree",
                table: "booking_concessions");
        }
    }
}

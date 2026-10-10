using ClosedXML.Excel;
using MotoGarage.Application.Interfaces.Services;
using MotoGarage.Domain.Entities;
using MotoGarage.Domain.Enums;
using MotoGarage.Domain.Exceptions;
using MotoGarage.Infrastructure.Exports;
using Microsoft.EntityFrameworkCore;

namespace MotoGarage.Tests;

public class VehicleHistoryExportTests
{
    static async Task Seed(MotoGarage.Infrastructure.Data.AppDbContext context)
    {
        context.Users.AddRange(new User { Id = 1, FullName = "Seller" }, new User { Id = 2, FullName = "Other owner" });
        context.Vehicles.AddRange(new Vehicle { Id = 1, UserId = 1, Slug = "toyota-corolla", Brand = "Toyota", Model = "Corolla", Year = 2022, Engine = "1.8", VIN = "000TESTVIN", PlateNumber = "10-AB-123", CurrentKm = 26000, VehicleType = VehicleType.Car },
            new Vehicle { Id = 2, UserId = 2, Slug = "private-car", Brand = "Private", Model = "Car", VIN = "PRIVATEVIN", PlateNumber = "PRIVATE", Engine = "2.0" });
        context.ServiceRecords.AddRange(new ServiceRecord { VehicleId = 1, ServiceName = "Yağ dəyişimi", Description = "Mühərrik yağı və filtr", Cost = 80.50m, Kilometer = 25000, ServiceDate = new DateTime(2026, 10, 5, 20, 15, 0, DateTimeKind.Utc) },
            new ServiceRecord { VehicleId = 1, ServiceName = "Əyləc dəsti", Cost = 120, Kilometer = 22000, ServiceDate = new DateTime(2026, 5, 1, 10, 0, 0, DateTimeKind.Utc) },
            new ServiceRecord { VehicleId = 2, ServiceName = "Private service", Cost = 999, Kilometer = 1, ServiceDate = DateTime.UtcNow });
        context.Expenses.Add(new Expense { VehicleId = 1, ExpenseType = ExpenseType.Insurance, Description = "Sığorta", Amount = 50.25m, ExpenseDate = DateTime.UtcNow });
        context.FuelRecords.Add(new FuelRecord { VehicleId = 1, Kilometer = 25100, Liters = 30.5m, PricePerLiter = 1.1m, TotalPrice = 33.55m, FuelDate = DateTime.UtcNow });
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
    }

    [Fact]
    public async Task WorkbookContainsOnlyOwnedVehicleHistoryWithTypedKilometersDatesMoneyAndWorkingTotals()
    {
        using var context = TestSupport.Context();
        await Seed(context);
        var mail = new HistoryEmail();
        var export = await new VehicleHistoryExportService(context, mail, TimeProvider.System).ExportAsync("toyota-corolla", 1);
        using var book = new XLWorkbook(new MemoryStream(export.Content));
        Assert.Equal(new[] { "Avtomobil", "Servis tarixçəsi", "Xərclər", "Yanacaq" }, book.Worksheets.Select(x => x.Name));
        Assert.Equal("toyota-corolla-tarixce.xlsx", export.FileName);
        var summary = book.Worksheet("Avtomobil");
        Assert.Equal("000TESTVIN", summary.Cell("B9").GetString());
        Assert.Equal(200.5, summary.Cell("B14").GetDouble());
        Assert.Equal(50.25, summary.Cell("B16").GetDouble());
        Assert.Equal(33.55, summary.Cell("B18").GetDouble());
        var services = book.Worksheet("Servis tarixçəsi");
        Assert.Equal("Əyləc dəsti", services.Cell("C5").GetString());
        Assert.Equal(25000, services.Cell("B6").GetDouble());
        Assert.Equal(XLDataType.Number, services.Cell("B6").DataType);
        Assert.Equal(new DateTime(2026, 10, 6, 0, 15, 0), services.Cell("A6").GetDateTime());
        Assert.Equal("dd.mm.yyyy hh:mm", services.Cell("A6").Style.NumberFormat.Format);
        Assert.Equal(200.5, services.Cell("E7").GetDouble());
        Assert.DoesNotContain(book.Worksheets.SelectMany(x => x.CellsUsed()), cell => cell.DataType == XLDataType.Text && cell.GetString().Contains("Private"));
        Assert.Null(mail.Recipient);
        Assert.Equal(3, await context.ServiceRecords.CountAsync());
        // Optional local sample for visual QA; never application/customer data.
        var sample = Environment.GetEnvironmentVariable("MOTOGARAGE_EXPORT_SAMPLE_PATH");
        if (!string.IsNullOrWhiteSpace(sample)) await File.WriteAllBytesAsync(sample, export.Content);
    }

    [Fact]
    public async Task AnotherOwnerCannotDownloadOrEmailHistory()
    {
        using var context = TestSupport.Context();
        await Seed(context);
        var mail = new HistoryEmail();
        var service = new VehicleHistoryExportService(context, mail, TimeProvider.System);
        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.ExportAsync("private-car", 1));
        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.SendAsync("private-car", 1, new() { RecipientEmail = "buyer@example.invalid", ConfirmSharing = true }));
        Assert.Null(mail.Recipient);
    }

    [Theory]
    [InlineData("", true)]
    [InlineData("bad-email", true)]
    [InlineData("a@example.invalid,b@example.invalid", true)]
    [InlineData("buyer@example.invalid\r\nBcc: victim@example.invalid", true)]
    [InlineData("buyer@example.invalid", false)]
    public async Task InvalidRecipientOrMissingConsentNeverSends(string recipient, bool consent)
    {
        using var context = TestSupport.Context();
        await Seed(context);
        var mail = new HistoryEmail();
        await Assert.ThrowsAsync<DomainException>(() => new VehicleHistoryExportService(context, mail, TimeProvider.System).SendAsync("toyota-corolla", 1, new() { RecipientEmail = recipient, ConfirmSharing = consent }));
        Assert.Null(mail.Recipient);
    }

    [Fact]
    public async Task ConfirmedRecipientReceivesExcelAttachmentAndSmtpFailuresAreNotRetried()
    {
        using var context = TestSupport.Context();
        await Seed(context);
        var mail = new HistoryEmail();
        var service = new VehicleHistoryExportService(context, mail, TimeProvider.System);
        await service.SendAsync("toyota-corolla", 1, new() { RecipientEmail = "buyer@example.invalid", ConfirmSharing = true });
        Assert.Equal("buyer@example.invalid", mail.Recipient);
        using var book = new XLWorkbook(new MemoryStream(mail.Attachment!));
        Assert.Equal(4, book.Worksheets.Count);
        mail.Fail = true;
        await Assert.ThrowsAsync<IOException>(() => service.SendAsync("toyota-corolla", 1, new() { RecipientEmail = "buyer@example.invalid", ConfirmSharing = true }));
        Assert.Equal(2, mail.Calls);
    }

    [Fact]
    public async Task EmptyHistoryAndUntrustedTextRemainValidAndDoNotBecomeFormulas()
    {
        using var context = TestSupport.Context();
        await Seed(context);
        context.ServiceRecords.RemoveRange(context.ServiceRecords);
        context.Expenses.RemoveRange(context.Expenses);
        context.FuelRecords.RemoveRange(context.FuelRecords);
        await context.SaveChangesAsync();
        var service = new VehicleHistoryExportService(context, new HistoryEmail(), TimeProvider.System);
        var empty = await service.ExportAsync("toyota-corolla", 1);
        using (var book = new XLWorkbook(new MemoryStream(empty.Content)))
        {
            Assert.Equal("Qeyd yoxdur", book.Worksheet("Servis tarixçəsi").Cell("A5").GetString());
            Assert.Equal(0, book.Worksheet("Avtomobil").Cell("B14").GetDouble());
        }
        context.ServiceRecords.Add(new ServiceRecord { VehicleId = 1, ServiceName = "=1+1", Description = "+SUM(1,2)", Kilometer = 0, ServiceDate = DateTime.UtcNow });
        await context.SaveChangesAsync();
        var file = await service.ExportAsync("toyota-corolla", 1);
        using var safe = new XLWorkbook(new MemoryStream(file.Content));
        var cell = safe.Worksheet("Servis tarixçəsi").Cell("C5");
        Assert.False(cell.HasFormula);
        Assert.Equal("=1+1", cell.GetString());
        Assert.Equal(0, safe.Worksheet("Servis tarixçəsi").Cell("B5").GetDouble());
    }

    sealed class HistoryEmail : IEmailService
    {
        public Task SendAppointmentReminderAsync(string email, MotoGarage.Application.DTOs.Appointment.AppointmentReminderEmail reminder, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public string? Recipient { get; private set; }
        public byte[]? Attachment { get; private set; }
        public int Calls { get; private set; }
        public bool Fail { get; set; }
        public Task SendVerificationCodeAsync(string email, string fullName, string code) => throw new NotSupportedException();
        public Task SendVehicleHistoryAsync(string email, string vehicleName, byte[] attachment, string fileName, CancellationToken cancellationToken = default)
        {
            Calls++; Recipient = email; Attachment = attachment;
            return Fail ? Task.FromException(new IOException("Simulated SMTP failure")) : Task.CompletedTask;
        }
    }
}

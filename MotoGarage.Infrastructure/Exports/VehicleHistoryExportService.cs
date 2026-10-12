using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using MotoGarage.Application.DTOs.Vehicle;
using MotoGarage.Application.Helpers;
using MotoGarage.Application.Interfaces.Services;
using MotoGarage.Domain.Enums;
using MotoGarage.Domain.Exceptions;
using MotoGarage.Infrastructure.Data;
using System.Net.Mail;

namespace MotoGarage.Infrastructure.Exports;

public sealed class VehicleHistoryExportService(AppDbContext context, IEmailService email, TimeProvider clock) : IVehicleHistoryExportService
{
    public async Task<VehicleHistoryFile> ExportAsync(string slug, int ownerId, CancellationToken cancellationToken = default)
    {
        var vehicle = await context.Vehicles.AsNoTracking().SingleOrDefaultAsync(x => x.Slug == slug && x.UserId == ownerId, cancellationToken)
            ?? throw new KeyNotFoundException("Nəqliyyat vasitəsi tapılmadı.");
        // Bounded queries avoid a cartesian join of the histories.
        const int limit = 10000;
        var services = await context.ServiceRecords.AsNoTracking().Where(x => x.VehicleId == vehicle.Id)
            .OrderBy(x => x.ServiceDate).ThenBy(x => x.Kilometer).ThenBy(x => x.Id).Take(limit + 1).ToListAsync(cancellationToken);
        var expenses = await context.Expenses.AsNoTracking().Where(x => x.VehicleId == vehicle.Id)
            .OrderBy(x => x.ExpenseDate).ThenBy(x => x.Id).Take(limit + 1).ToListAsync(cancellationToken);
        var fuels = await context.FuelRecords.AsNoTracking().Where(x => x.VehicleId == vehicle.Id)
            .OrderBy(x => x.FuelDate).ThenBy(x => x.Kilometer).ThenBy(x => x.Id).Take(limit + 1).ToListAsync(cancellationToken);
        if (services.Count > limit || expenses.Count > limit || fuels.Count > limit)
            throw new DomainException("Hesabat üçün hər bölmədə maksimum 10 000 qeyd ola bilər. Dəstək ilə əlaqə saxlayın.");
        cancellationToken.ThrowIfCancellationRequested();
        using var book = new XLWorkbook();
        book.Style.Font.FontName = "Calibri";
        book.Style.Font.FontSize = 11;
        var summary = Sheet(book, "Avtomobil", "Nəqliyyat vasitəsinin tarixçəsi", ["Məlumat", "Dəyər"], [32, 66]);
        var row = 5;
        void Info(string name, XLCellValue value) { summary.Cell(row, 1).Value = name; summary.Cell(row++, 2).Value = value; }
        Info("Marka və model", $"{vehicle.Brand} {vehicle.Model}");
        Info("İl", vehicle.Year);
        Info("Növ", vehicle.VehicleType == VehicleType.Car ? "Avtomobil" : "Motosiklet");
        Info("Mühərrik", vehicle.Engine);
        Info("VIN", vehicle.VIN);
        Info("Dövlət nömrəsi", vehicle.PlateNumber);
        Info("Cari odometr (km)", vehicle.CurrentKm);
        summary.Cell(11, 2).Style.NumberFormat.Format = "#,##0";
        Info("Hesabat tarixi (Bakı vaxtı)", Baku(clock.GetUtcNow().UtcDateTime));
        summary.Cell(12, 2).Style.NumberFormat.Format = "dd.mm.yyyy hh:mm";
        Info("Servis qeydlərinin sayı", services.Count);
        Info("Servis məbləğləri (AZN)", 0);
        Info("Xərc qeydlərinin sayı", expenses.Count);
        Info("Xərc məbləğləri (AZN)", 0);
        Info("Yanacaq qeydlərinin sayı", fuels.Count);
        Info("Yanacaq məbləğləri (AZN)", 0);
        foreach (var moneyRow in new[] { 14, 16, 18 }) summary.Cell(moneyRow, 2).Style.NumberFormat.Format = "#,##0.00";
        Info("Mənbə", "MotoGarage-də sahibin daxil etdiyi qeydlər");
        Info("Məbləğlər haqqında", "Servis, xərc və yanacaq məbləğləri ayrı hesablanıb. Eyni ödəniş müxtəlif bölmələrdə qeyd edilə bilər.");
        Info("Xərc kilometri haqqında", "Xərc qeydlərində odometr saxlanmır. Kilometr servis və yanacaq vərəqlərində göstərilir.");
        Finish(summary, row - 1, 2);

        var serviceSheet = Sheet(book, "Servis tarixçəsi", "Görülən işlər və kilometr", ["Tarix (Bakı vaxtı)", "Kilometr (km)", "Görülən iş", "Açıqlama", "Məbləğ (AZN)"], [24, 19, 32, 64, 20]);
        row = 5;
        foreach (var record in services)
        {
            serviceSheet.Cell(row, 1).Value = Baku(record.ServiceDate);
            serviceSheet.Cell(row, 2).Value = record.Kilometer;
            serviceSheet.Cell(row, 3).Value = record.ServiceName;
            serviceSheet.Cell(row, 4).Value = string.Join("\n", new[] { record.Description, string.IsNullOrWhiteSpace(record.ReplacedParts) ? null : "Dəyişən hissələr: " + record.ReplacedParts }.Where(x => !string.IsNullOrWhiteSpace(x)));
            serviceSheet.Cell(row++, 5).Value = record.Cost;
        }
        DateAndMoney(serviceSheet, 5); serviceSheet.Column(2).Style.NumberFormat.Format = "#,##0";
        Total(serviceSheet, row, 5); Finish(serviceSheet, row, 5);
        summary.Cell(14, 2).FormulaA1 = $"'Servis tarixçəsi'!E{row}";

        var expenseSheet = Sheet(book, "Xərclər", "Xərc qeydləri", ["Tarix (Bakı vaxtı)", "Kateqoriya", "Açıqlama", "Məbləğ (AZN)"], [24, 22, 64, 20]);
        row = 5;
        foreach (var record in expenses)
        {
            expenseSheet.Cell(row, 1).Value = Baku(record.ExpenseDate);
            expenseSheet.Cell(row, 2).Value = ExpenseLabel(record.ExpenseType);
            expenseSheet.Cell(row, 3).Value = record.Description;
            expenseSheet.Cell(row++, 4).Value = record.Amount;
        }
        DateAndMoney(expenseSheet, 4); Total(expenseSheet, row, 4); Finish(expenseSheet, row, 4);
        summary.Cell(16, 2).FormulaA1 = $"'Xərclər'!D{row}";

        var fuelSheet = Sheet(book, "Yanacaq", "Yanacaq doldurmaları", ["Tarix (Bakı vaxtı)", "Kilometr (km)", "Litr", "Litr qiyməti (AZN)", "Məbləğ (AZN)"], [24, 19, 15, 23, 20]);
        row = 5;
        foreach (var record in fuels)
        {
            fuelSheet.Cell(row, 1).Value = Baku(record.FuelDate);
            fuelSheet.Cell(row, 2).Value = record.Kilometer;
            fuelSheet.Cell(row, 3).Value = record.Liters;
            fuelSheet.Cell(row, 4).Value = record.PricePerLiter;
            fuelSheet.Cell(row++, 5).Value = record.TotalPrice;
        }
        DateAndMoney(fuelSheet, 5); fuelSheet.Column(2).Style.NumberFormat.Format = "#,##0";
        fuelSheet.Columns(3, 4).Style.NumberFormat.Format = "#,##0.00";
        Total(fuelSheet, row, 5); Finish(fuelSheet, row, 5);
        summary.Cell(18, 2).FormulaA1 = $"'Yanacaq'!E{row}";
        using var stream = new MemoryStream();
        book.SaveAs(stream, new SaveOptions { EvaluateFormulasBeforeSaving = true });
        if (stream.Length > 10 * 1024 * 1024) throw new DomainException("Hesabat email əlavəsi üçün çox böyükdür (maksimum 10 MB).");
        return new(stream.ToArray(), $"{vehicle.Slug}-tarixce.xlsx", $"{vehicle.Brand} {vehicle.Model}");
    }

    public async Task SendAsync(string slug, int ownerId, SendVehicleHistoryDto dto, CancellationToken cancellationToken = default)
    {
        DtoValidation.Validate(dto);
        if (!dto.ConfirmSharing) throw new DomainException("Tarixçənin bu email ünvanına göndərilməsini təsdiqləyin.");
        var recipient = dto.RecipientEmail.Trim();
        if (!MailAddress.TryCreate(recipient, out var address) || !string.Equals(address.Address, recipient, StringComparison.OrdinalIgnoreCase))
            throw new DomainException("Alıcının email ünvanı düzgün deyil.");
        var file = await ExportAsync(slug, ownerId, cancellationToken);
        // No automatic retries around SMTP, which could send duplicate messages.
        await email.SendVehicleHistoryAsync(recipient, file.VehicleName, file.Content, file.FileName, cancellationToken);
    }

    static DateTime Baku(DateTime date) => DateTime.SpecifyKind(date, DateTimeKind.Utc).AddHours(4);
    static string ExpenseLabel(ExpenseType type) => type switch
    { ExpenseType.Fuel => "Yanacaq", ExpenseType.Service => "Servis", ExpenseType.SparePart => "Ehtiyat hissəsi", ExpenseType.Insurance => "Sığorta", _ => "Digər" };

    static IXLWorksheet Sheet(XLWorkbook book, string name, string title, string[] headers, int[] widths)
    {
        var sheet = book.Worksheets.Add(name);
        sheet.ShowGridLines = false;
        sheet.Cell(1, 1).Value = title;
        sheet.Range(1, 1, 1, headers.Length).Merge().Style.Font.SetBold().Font.SetFontSize(18);
        sheet.Row(1).Height = 36;
        sheet.Cell(2, 1).Value = "MotoGarage";
        for (var col = 1; col <= headers.Length; col++)
        {
            sheet.Cell(4, col).Value = headers[col - 1];
            sheet.Column(col).Width = widths[col - 1];
        }
        var header = sheet.Range(4, 1, 4, headers.Length);
        header.Style.Fill.BackgroundColor = XLColor.FromHtml("#2479F3");
        header.Style.Font.FontColor = XLColor.White;
        header.Style.Font.Bold = true;
        sheet.Row(4).Height = 26;
        sheet.SheetView.FreezeRows(4);
        return sheet;
    }
    static void DateAndMoney(IXLWorksheet sheet, int moneyColumn)
    {
        sheet.Column(1).Style.NumberFormat.Format = "dd.mm.yyyy hh:mm";
        sheet.Column(moneyColumn).Style.NumberFormat.Format = "#,##0.00";
    }
    static void Total(IXLWorksheet sheet, int row, int moneyColumn)
    {
        sheet.Cell(row, 1).Value = row == 5 ? "Qeyd yoxdur" : "Cəmi";
        if (row == 5) sheet.Cell(row, moneyColumn).Value = 0;
        else sheet.Cell(row, moneyColumn).FormulaA1 = $"SUM({sheet.Column(moneyColumn).ColumnLetter()}5:{sheet.Column(moneyColumn).ColumnLetter()}{row - 1})";
        sheet.Row(row).Style.Font.Bold = true;
        sheet.Row(row).Style.Fill.BackgroundColor = XLColor.FromHtml("#EFF3F8");
        if (row > 5) sheet.Range(4, 1, row - 1, moneyColumn).SetAutoFilter();
    }
    static void Finish(IXLWorksheet sheet, int lastRow, int columns)
    {
        sheet.Range(5, 1, lastRow, columns).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        foreach (var cell in sheet.Range(5, 1, lastRow, columns).CellsUsed())
            if (cell.DataType == XLDataType.Text)
            {
                var text = cell.GetString();
                var width = sheet.Column(cell.Address.ColumnNumber).Width * 0.85;
                var lines = text.Split('\n').Sum(line => Math.Max(1, Math.Ceiling(line.Length / width)));
                if (lines > 1)
                {
                    cell.Style.Alignment.WrapText = true;
                    sheet.Row(cell.Address.RowNumber).Height = Math.Min(409, Math.Max(sheet.Row(cell.Address.RowNumber).Height, lines * 16 + 8));
                }
            }
        sheet.PageSetup.PageOrientation = XLPageOrientation.Landscape;
        sheet.PageSetup.FitToPages(1, 0);
        sheet.PageSetup.SetRowsToRepeatAtTop(1, 4);
        sheet.PageSetup.PrintAreas.Add(1, 1, lastRow, columns);
    }
}

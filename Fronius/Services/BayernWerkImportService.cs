namespace De.Hochstaetter.Fronius.Services;

public class BayernWerkImportService(SettingsBase settings, IDataCollectionService dataCollectionService) : ISmartMeterImportService
{
    public async Task ImportSmartMeterData(object parameters)
    {
        if (parameters is not string excelFileName)
        {
            throw new ArgumentException(string.Format(Resources.MustBeExcelFileName, nameof(parameters)), nameof(parameters));
        }

        var energyHistoryFileName = settings.EnergyHistoryFileName ?? throw new FileNotFoundException(Resources.NoEnergyHistoryFile);
        _ = settings.DriftFileName ?? throw new FileNotFoundException(Resources.NoDriftFile);

        // ClosedXML workbook parsing and XML deserialization are synchronous and CPU-bound, so yield off the calling (UI) thread before doing them.
        await Task.CompletedTask.ConfigureAwait(ConfigureAwaitOptions.ForceYielding);

        IReadOnlyList<SmartMeterCalibrationHistoryItem> energyHistory;

        await using (var fileStream = new FileStream(energyHistoryFileName, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            var energyHistorySerializer = new XmlSerializer(typeof(SmartMeterCalibrationHistoryItem[]));

            energyHistory = energyHistorySerializer.Deserialize(fileStream) as IReadOnlyList<SmartMeterCalibrationHistoryItem>
                            ?? throw new InvalidDataException(string.Format(Resources.FileHasNoEnergyHistory, energyHistoryFileName));
        }

        await using var xlFileStream = new FileStream(excelFileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var workbook = new XLWorkbook(xlFileStream);
        var sheet = workbook.Worksheet(1);
        var obisCode = sheet.Cell("C4").Value.GetText();

        var isProduction = obisCode switch
        {
            "1-1:1.29.0" => false,
            "1-1:2.29.0" => true,
            _ => throw new InvalidDataException("Incorrect OBIS code in Cell C4 of Excel file"),
        };

        var cell = sheet.Column("F").CellsUsed()
                       .Where(c => c.Value.IsText && c.GetValue<string>() == "W" && c.WorksheetRow().Cell("G") is { Value.IsNumber: true } valueCell && Math.Abs(valueCell.GetValue<double>()) > .00001)
                       .MaxBy(c => c.WorksheetRow().RowNumber())
                   ?? throw new InvalidDataException(Resources.NoValidCells);

        var row = cell.WorksheetRow();
        var energy = row.Cell("G").GetValue<double>() * 1000;

        var time = new DateTime(row.Cell("A").GetDateTime().Ticks, DateTimeKind.Utc);
        var historyEntry = energyHistory.MinBy(i => Math.Abs(i.CalibrationDate.Ticks - time.Ticks));

        if (historyEntry == null || Math.Abs((historyEntry.CalibrationDate - time).TotalMinutes) > 2)
        {
            throw new InvalidOperationException(Resources.NoEnergyHistoryMatch);
        }

        historyEntry.ProducedOffset = isProduction ? energy - historyEntry.EnergyRealProduced : double.NaN;
        historyEntry.ConsumedOffset = isProduction ? double.NaN : energy - historyEntry.EnergyRealConsumed;

        var lastEntry = dataCollectionService.SmartMeterHistory.LastOrDefault(i => double.IsFinite(isProduction ? i.ProducedOffset : i.ConsumedOffset));

        if (lastEntry != null && lastEntry.CalibrationDate >= historyEntry.CalibrationDate)
        {
            throw new InvalidOperationException(Resources.ExcelFileAlreadyImported);
        }

        await dataCollectionService.AddCalibrationHistoryItem(historyEntry).ConfigureAwait(false);
    }
}

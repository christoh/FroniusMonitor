namespace De.Hochstaetter.Fronius.Services
{
    public class BayernWerkImportService(SettingsBase settings, IDataCollectionService dataCollectionService) : ISmartMeterImportService
    {
        public async Task ImportSmartMeterData(object parameters)
        {
            if (parameters is not string excelFileName)
            {
                throw new ArgumentException(string.Format(Resources.MustBeExcelFileName, nameof(parameters)), nameof(parameters));
            }

            IReadOnlyList<SmartMeterCalibrationHistoryItem> energyHistory;

            _ = settings.EnergyHistoryFileName ?? throw new FileNotFoundException(Resources.NoEnergyHistoryFile);
            _ = settings.DriftFileName ?? throw new FileNotFoundException(Resources.NoDriftFile);

            await using (var fileStream = new FileStream(settings.EnergyHistoryFileName, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                var energyHistorySerializer = new XmlSerializer(typeof(SmartMeterCalibrationHistoryItem[]));

                energyHistory = energyHistorySerializer.Deserialize(fileStream) as IReadOnlyList<SmartMeterCalibrationHistoryItem>
                                ?? throw new InvalidDataException(string.Format(Resources.FileHasNoEnergyHistory, settings.EnergyHistoryFileName));
            }

            XLWorkbook workbook;

            await using (var fileStream = new FileStream(excelFileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                workbook = new XLWorkbook(fileStream);
            }

            var sheet = workbook.Worksheet(1);
            var obisCode = sheet.Cell("C4").Value.GetText();

            var isProduction = obisCode switch
            {
                "1-1:1.29.0" => false,
                "1-1:2.29.0" => true,
                _ => throw new InvalidDataException("Incorrect OBIS code in Cell C4 of Excel file"),
            };

            var cell = sheet.Range("F:F").Cells()
                           .Where(c => c.Value.IsText && c.GetValue<string>() == "VAL")
                           .MaxBy(c => c.WorksheetRow().RowNumber())
                       ?? throw new InvalidDataException(Resources.NoValidCells);

            var row = cell.WorksheetRow();
            var energy = row.Cell("G").GetValue<double>() * 1000;
            var time = row.Cell("C").GetDateTime().ToUniversalTime();
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
}

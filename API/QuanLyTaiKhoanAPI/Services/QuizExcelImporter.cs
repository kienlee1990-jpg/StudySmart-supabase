using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Xml.Linq;

namespace QuanLyTaiKhoanAPI.Services;

public static class QuizExcelImporter
{
    private static readonly XNamespace Spreadsheet = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
    private static readonly XNamespace Relationships = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
    private static readonly XNamespace PackageRelationships = "http://schemas.openxmlformats.org/package/2006/relationships";
    private const long MaxExpandedBytes = 25_000_000;
    private const int MaxRows = 501;

    public static List<ImportedQuestionRow> Read(Stream stream)
    {
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true);
        if (archive.Entries.Count > 200 || archive.Entries.Sum(entry => entry.Length) > MaxExpandedBytes)
            throw new InvalidDataException("Tệp Excel vượt quá giới hạn kích thước cho phép.");

        var workbook = ReadXml(archive, "xl/workbook.xml");
        var firstSheet = workbook.Root?.Element(Spreadsheet + "sheets")?.Elements(Spreadsheet + "sheet").FirstOrDefault()
            ?? throw new InvalidDataException("Tệp Excel không có trang tính.");
        var relationshipId = (string?)firstSheet.Attribute(Relationships + "id")
            ?? throw new InvalidDataException("Không đọc được trang tính đầu tiên.");
        var relationships = ReadXml(archive, "xl/_rels/workbook.xml.rels");
        var target = (string?)relationships.Root?.Elements(PackageRelationships + "Relationship")
            .FirstOrDefault(item => (string?)item.Attribute("Id") == relationshipId)?.Attribute("Target")
            ?? throw new InvalidDataException("Không tìm thấy dữ liệu trang tính.");
        if (target.Contains(":", StringComparison.Ordinal) || target.StartsWith("//", StringComparison.Ordinal))
            throw new InvalidDataException("Liên kết trang tính trong tệp không hợp lệ.");
        var sheetPath = target.StartsWith('/') ? target.TrimStart('/') : "xl/" + target.TrimStart('/');
        sheetPath = NormalizeEntryPath(sheetPath);
        var sheet = ReadXml(archive, sheetPath);
        var sharedStringsEntry = archive.GetEntry("xl/sharedStrings.xml");
        List<string> sharedStrings = sharedStringsEntry == null
            ? new List<string>()
            : ReadXml(sharedStringsEntry).Root?.Elements(Spreadsheet + "si")
                .Select(item => string.Concat(item.Descendants(Spreadsheet + "t").Select(text => text.Value))).ToList() ?? new List<string>();

        var rows = sheet.Descendants(Spreadsheet + "sheetData").Elements(Spreadsheet + "row").Take(MaxRows + 1).ToList();
        if (rows.Count > MaxRows) throw new InvalidDataException($"Mỗi tệp chỉ được có tối đa 500 câu hỏi.");
        var matrix = rows.Select(row => row.Elements(Spreadsheet + "c").ToDictionary(
            cell => ColumnIndex((string?)cell.Attribute("r") ?? ""), cell => CellValue(cell, sharedStrings)))
            .ToList();
        var headerRow = matrix.FindIndex(row => row.Values.Any(value => !string.IsNullOrWhiteSpace(value)));
        if (headerRow < 0) throw new InvalidDataException("Trang tính chưa có dòng tiêu đề.");

        var headers = matrix[headerRow].ToDictionary(
            cell => NormalizeHeader(cell.Value), cell => cell.Key);
        int Header(string name) => headers.TryGetValue(NormalizeHeader(name), out var index) ? index : -1;
        var required = new[] { "Lớp", "Môn", "Nội dung câu hỏi", "Đáp án A", "Đáp án B", "Đáp án C", "Đáp án D", "Đáp án đúng" };
        var missing = required.Where(name => Header(name) < 0).ToList();
        if (missing.Count > 0) throw new InvalidDataException("Thiếu cột bắt buộc: " + string.Join(", ", missing));

        var results = new List<ImportedQuestionRow>();
        for (var index = headerRow + 1; index < matrix.Count; index++)
        {
            var row = matrix[index];
            string Value(string header) => row.GetValueOrDefault(Header(header), "").Trim();
            if (row.Values.All(string.IsNullOrWhiteSpace)) continue;
            if (!int.TryParse(Value("Lớp"), NumberStyles.Integer, CultureInfo.InvariantCulture, out var grade) || grade is < 5 or > 12)
                throw new InvalidDataException($"Dòng {index + 1}: cột Lớp phải từ 5 đến 12.");
            var subject = Value("Môn");
            if (subject is not ("Toán" or "Tiếng Anh" or "Tiếng Việt"))
                throw new InvalidDataException($"Dòng {index + 1}: cột Môn phải là Toán, Tiếng Anh hoặc Tiếng Việt.");
            var content = Value("Nội dung câu hỏi");
            var options = new[] { Value("Đáp án A"), Value("Đáp án B"), Value("Đáp án C"), Value("Đáp án D") };
            var correct = Value("Đáp án đúng").Trim().TrimEnd('.').ToUpperInvariant();
            var difficulty = string.IsNullOrWhiteSpace(Value("Loại câu hỏi")) ? "Thường" : Value("Loại câu hỏi");
            var topic = Value("Chủ đề");
            if (topic.Length > 200)
                throw new InvalidDataException($"Dòng {index + 1}: Chủ đề tối đa 200 ký tự.");
            if (difficulty is not ("Thường" or "Nâng cao"))
                throw new InvalidDataException($"Dòng {index + 1}: Loại câu hỏi phải là Thường hoặc Nâng cao.");
            if (string.IsNullOrWhiteSpace(content) || content.Length > 4000 || options.Any(string.IsNullOrWhiteSpace) || options.Any(option => option.Length > 1000))
                throw new InvalidDataException($"Dòng {index + 1}: câu hỏi và 4 đáp án là bắt buộc; câu hỏi tối đa 4000 ký tự, đáp án tối đa 1000 ký tự.");
            if (correct.Length != 1 || correct[0] is < 'A' or > 'D')
                throw new InvalidDataException($"Dòng {index + 1}: cột Đáp án đúng phải là A, B, C hoặc D.");
            if (options.Distinct(StringComparer.OrdinalIgnoreCase).Count() != options.Length)
                throw new InvalidDataException($"Dòng {index + 1}: các đáp án phải khác nhau.");
            results.Add(new ImportedQuestionRow(grade, subject, content, options, correct[0] - 'A', Value("Giải thích"), difficulty, topic));
        }
        if (results.Count == 0) throw new InvalidDataException("Không có dòng câu hỏi nào để nhập.");
        return results;
    }

    public static byte[] CreateTemplate()
    {
        using var output = new MemoryStream();
        using (var archive = new ZipArchive(output, ZipArchiveMode.Create, leaveOpen: true))
        {
            WriteEntry(archive, "[Content_Types].xml", """
                <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
                <Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types"><Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/><Default Extension="xml" ContentType="application/xml"/><Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"/><Override PartName="/xl/worksheets/sheet1.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/><Override PartName="/xl/worksheets/sheet2.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/></Types>
                """);
            WriteEntry(archive, "_rels/.rels", """
                <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
                <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="xl/workbook.xml"/></Relationships>
                """);
            WriteEntry(archive, "xl/workbook.xml", """
                <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
                <workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships"><sheets><sheet name="CauHoi" sheetId="1" r:id="rId1"/><sheet name="HuongDan" sheetId="2" r:id="rId2"/></sheets></workbook>
                """);
            WriteEntry(archive, "xl/_rels/workbook.xml.rels", """
                <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
                <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet1.xml"/><Relationship Id="rId2" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet2.xml"/></Relationships>
                """);
            var headers = new[] { "Lớp", "Môn", "Chủ đề", "Nội dung câu hỏi", "Đáp án A", "Đáp án B", "Đáp án C", "Đáp án D", "Đáp án đúng", "Giải thích", "Loại câu hỏi" };
            var headerCells = string.Join("", headers.Select((value, index) => InlineCell(index, 1, value)));
            WriteEntry(archive, "xl/worksheets/sheet1.xml", $"""
                <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
                <worksheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main"><sheetViews><sheetView workbookViewId="0"><pane ySplit="1" topLeftCell="A2" activePane="bottomLeft" state="frozen"/></sheetView></sheetViews><sheetData><row r="1">{headerCells}</row><row r="2"/></sheetData></worksheet>
                """);
            var guide = new (int Column, int Row, string Value)[]
            {
                (0, 1, "Hướng dẫn điền mẫu import câu hỏi"),
                (0, 2, "Nhập câu hỏi trong sheet CauHoi, bắt đầu từ dòng 2. Không đổi tên hoặc xóa các cột tiêu đề."),
                (0, 3, "Các cột Lớp, Môn, Nội dung câu hỏi, Đáp án A, B, C, D và Đáp án đúng là bắt buộc."),
                (0, 4, "Đáp án đúng ghi một chữ cái A, B, C hoặc D; bốn lựa chọn phải có nội dung khác nhau."),
                (0, 5, "Loại câu hỏi chỉ nhận Thường hoặc Nâng cao. Nếu để trống, hệ thống mặc định là Thường."),
                (0, 6, "Chủ đề không bắt buộc, tối đa 200 ký tự; có thể dùng tên mạch hoặc chủ đề con."),
                (0, 7, "Giới hạn: câu hỏi tối đa 4.000 ký tự; mỗi đáp án tối đa 1.000 ký tự; tối đa 500 câu và 5 MB mỗi lần."),
                (0, 8, "Các dòng trống được bỏ qua. Sheet HuongDan không được dùng làm nơi nhập câu hỏi.")
            };
            var guideRows = guide.GroupBy(item => item.Row)
                .Select(row => $"<row r=\"{row.Key}\">{string.Join("", row.Select(item => InlineCell(item.Column, item.Row, item.Value)))}</row>");
            WriteEntry(archive, "xl/worksheets/sheet2.xml", $"""
                <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
                <worksheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main"><sheetData>{string.Join("", guideRows)}</sheetData></worksheet>
                """);
        }
        return output.ToArray();
    }

    private static string CellValue(XElement cell, List<string> sharedStrings)
    {
        var type = (string?)cell.Attribute("t");
        if (type == "inlineStr") return string.Concat(cell.Descendants(Spreadsheet + "t").Select(text => text.Value));
        var value = cell.Element(Spreadsheet + "v")?.Value ?? "";
        if (type == "s" && int.TryParse(value, out var sharedIndex) && sharedIndex >= 0 && sharedIndex < sharedStrings.Count)
            return sharedStrings[sharedIndex];
        return value;
    }

    private static XDocument ReadXml(ZipArchive archive, string path)
        => ReadXml(archive.GetEntry(path) ?? throw new InvalidDataException($"Tệp Excel thiếu thành phần {path}."));

    private static XDocument ReadXml(ZipArchiveEntry entry)
    {
        using var stream = entry.Open();
        return XDocument.Load(stream);
    }

    private static string NormalizeEntryPath(string path)
    {
        var segments = new List<string>();
        foreach (var segment in path.Replace('\\', '/').Split('/'))
        {
            if (segment is "" or ".") continue;
            if (segment == "..")
            {
                if (segments.Count == 0) throw new InvalidDataException("Đường dẫn trang tính Excel không hợp lệ.");
                segments.RemoveAt(segments.Count - 1);
            }
            else segments.Add(segment);
        }
        return string.Join('/', segments);
    }

    private static int ColumnIndex(string cellReference)
    {
        var value = 0;
        foreach (var character in cellReference.TakeWhile(char.IsLetter))
            value = value * 26 + char.ToUpperInvariant(character) - 'A' + 1;
        return Math.Max(0, value - 1);
    }

    private static string NormalizeHeader(string value)
        => string.Concat(value.Where(char.IsLetterOrDigit)).ToLowerInvariant();

    private static string InlineCell(int column, int row, string value)
    {
        var columnName = "";
        for (var number = column + 1; number > 0; number = (number - 1) / 26)
            columnName = (char)('A' + (number - 1) % 26) + columnName;
        var escaped = System.Security.SecurityElement.Escape(value) ?? "";
        return $"<c r=\"{columnName}{row}\" t=\"inlineStr\"><is><t xml:space=\"preserve\">{escaped}</t></is></c>";
    }

    private static void WriteEntry(ZipArchive archive, string path, string content)
    {
        var entry = archive.CreateEntry(path, CompressionLevel.Optimal);
        using var writer = new StreamWriter(entry.Open(), new UTF8Encoding(false));
        writer.Write(content.Trim());
    }
}

public sealed record ImportedQuestionRow(int Grade, string Subject, string Content, string[] Options, int CorrectIndex, string Explanation, string Difficulty = "Thường", string? Topic = null);

using QRCoder;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

public class SaleOutPdfDocument : IDocument
{
    private readonly List<SaleOutPdf> _rows;
    private readonly string _saleOutNo;

    public SaleOutPdfDocument(List<SaleOutPdf> row, string saleOutNo)
    {
        _rows = row;
        _saleOutNo = saleOutNo;
    }

    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    public void Compose(IDocumentContainer container)
    {
        var header = _rows.First();

        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(20);
            page.DefaultTextStyle(x => x.FontSize(11));

            page.Content().Column(col =>
            {
                col.Item().Row(row =>
                {
                    row.RelativeItem().Column(left =>
                    {
                        left.Item()
                            .Text("PHIẾU XUẤT HÀNG")
                            .FontSize(18)
                            .Bold();

                        left.Item().PaddingTop(8).Text(text =>
                        {
                            text.Span("Khách hàng: ").Bold();
                            text.Span(header.CustomerName);
                        });

                        left.Item().Text(text =>
                        {
                            text.Span("Số phiếu: ").Bold();
                            text.Span(_saleOutNo);
                        });

                        left.Item().Text(text =>
                        {
                            text.Span("Ngày xuất kho: ").Bold();
                            text.Span(DateHelper.formatNumbertoDateString(header.OrderDate));
                        });
                    });

                    row.ConstantItem(90)
                        .AlignMiddle()
                        .AlignRight()
                        .Column(col2 =>
                        {
                            col2.Item()
                                .AlignCenter()
                                .Width(70)
                                .Height(70)
                                .Image(GenerateQr(_saleOutNo));

                            col2.Item()
                                .AlignCenter()
                                .Text(_saleOutNo)
                                .FontSize(8);
                        });
                });



                col.Item().PaddingVertical(10);

                col.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(35);
                        columns.RelativeColumn(2);
                        columns.RelativeColumn(4);
                        columns.RelativeColumn(2);
                        columns.RelativeColumn(2);
                        columns.RelativeColumn(2);
                    });

                    // ===== HEADER =====
                    table.Header(header =>
                    {
                        header.Cell().Element(HeaderCell).Text("STT");
                        header.Cell().Element(HeaderCell).Text("Mã SP");
                        header.Cell().Element(HeaderCell).Text("Tên sản phẩm");
                        header.Cell().Element(HeaderCell).AlignRight().Text("Số lượng");
                        header.Cell().Element(HeaderCell).AlignRight().Text("Đơn giá");
                        header.Cell().Element(HeaderCell).AlignRight().Text("Thành tiền");
                    });

                    int i = 1;
                    foreach (var r in _rows)
                    {
                        table.Cell().Element(BodyCell).Text(i++.ToString()).Bold();
                        table.Cell().Element(BodyCell).Text(r.ProductCode);
                        table.Cell().Element(BodyCell).Text(r.ProductName);
                        table.Cell().Element(BodyCell).AlignRight().Text(r.Quantity.ToString("N0"));
                        table.Cell().Element(BodyCell).AlignRight().Text(r.Price.ToString("N0"));
                        table.Cell().Element(BodyCell).AlignRight().Text(r.Amount.ToString("N0"));
                    }
                    var totalAmount = _rows.Sum(x => x.Amount);
                    var totalQuantity = _rows.Sum(x => x.Quantity);

                    table.Cell().ColumnSpan(3).Element(BodyCell).AlignCenter().Text("Tổng:").Bold();

                    table.Cell().Element(BodyCell).AlignRight().Text(totalQuantity.ToString("N0")).Bold();

                    table.Cell().Element(BodyCell).AlignRight().Text("");

                    table.Cell().Element(BodyCell).AlignRight().Text(totalAmount.ToString("N0")).Bold();

                });


                col.Item().PaddingTop(30).Row(row =>
                {
                    row.RelativeItem().AlignCenter().Text("Người lập phiếu").Bold();
                    row.RelativeItem().AlignCenter().Text("Người duyệt").Bold();
                    row.RelativeItem().AlignCenter().Text("Thủ kho").Bold();
                    row.RelativeItem().AlignCenter().Text("Người nhận").Bold();
                });
            });
        });
    }
    static IContainer HeaderCell(IContainer container)
    {
        return container
            .Border(1)
            .BorderColor(Colors.Grey.Lighten2)
            .Background(Colors.Grey.Lighten4)
            .PaddingVertical(6)
            .PaddingHorizontal(4)
            .AlignMiddle()
            .DefaultTextStyle(x => x.Bold());
    }

    static IContainer BodyCell(IContainer container)
    {
        return container
            .Border(1)
            .BorderColor(Colors.Grey.Lighten2)
            .PaddingVertical(5)
            .PaddingHorizontal(4)
            .AlignMiddle();
    }
    private static byte[] GenerateQr(string content)
    {
        using var qrGenerator = new QRCodeGenerator();
        using var qrData = qrGenerator.CreateQrCode(content, QRCodeGenerator.ECCLevel.Q);

        var qrCode = new PngByteQRCode(qrData);
        return qrCode.GetGraphic(6);
    }
}

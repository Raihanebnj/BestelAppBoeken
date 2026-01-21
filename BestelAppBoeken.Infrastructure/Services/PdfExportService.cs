using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BestelAppBoeken.Core.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace BestelAppBoeken.Infrastructure.Services
{
    public class PdfExportService
    {
        public byte[] GenerateOrdersPdf(List<Order> orders)
        {
            // Set QuestPDF license (Community license for open-source/non-commercial use)
            QuestPDF.Settings.License = LicenseType.Community;

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(1.5f, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                    // Compact Header
                    page.Header()
                        .BorderBottom(2)
                        .BorderColor(Colors.Grey.Darken2)
                        .PaddingBottom(10)
                        .Row(row =>
                        {
                            // Left: Company info
                            row.RelativeItem().Column(col =>
                            {
                                col.Item().Text("?? BestelAppBoeken")
                                    .FontSize(18)
                                    .Bold()
                                    .FontColor(Colors.Blue.Darken2);

                                col.Item().Text("Nijverheidskaai 170, 1070 Brussel")
                                    .FontSize(8);
                            });

                            // Right: Document info
                            row.RelativeItem().AlignRight().Column(col =>
                            {
                                col.Item().AlignRight().Text("BESTELLINGEN OVERZICHT")
                                    .FontSize(14)
                                    .Bold()
                                    .FontColor(Colors.Blue.Darken2);

                                col.Item().AlignRight().Text($"{DateTime.Now:dd MMMM yyyy}")
                                    .FontSize(8);
                            });
                        });

                    // Content
                    page.Content()
                        .PaddingVertical(10)
                        .Column(column =>
                        {
                            // Summary Box
                            column.Item().Border(1).BorderColor(Colors.Grey.Lighten1)
                                .Background(Colors.Grey.Lighten4)
                                .Padding(10)
                                .Row(summaryRow =>
                                {
                                    summaryRow.RelativeItem().Text($"Totaal: {orders.Count} bestellingen")
                                        .FontSize(10)
                                        .Bold();

                                    summaryRow.RelativeItem().AlignRight().Text($"Bedrag: € {orders.Sum(o => o.TotalAmount):N2}")
                                        .FontSize(10)
                                        .Bold()
                                        .FontColor(Colors.Green.Darken1);
                                });

                            // Orders List
                            foreach (var order in orders.OrderByDescending(o => o.OrderDate))
                            {
                                column.Item().PaddingTop(10).Border(1).BorderColor(Colors.Grey.Lighten1)
                                    .Padding(10)
                                    .Column(orderCol =>
                                    {
                                        // Order Header
                                        orderCol.Item().BorderBottom(1).BorderColor(Colors.Grey.Lighten2)
                                            .PaddingBottom(5)
                                            .Row(headerRow =>
                                            {
                                                headerRow.RelativeItem().Text($"Order #{order.Id}")
                                                    .FontSize(10)
                                                    .Bold();

                                                headerRow.RelativeItem().AlignRight().Text($"{order.OrderDate:dd-MM-yyyy HH:mm}")
                                                    .FontSize(9);
                                            });

                                        // Customer & Amount
                                        orderCol.Item().PaddingTop(5).Row(infoRow =>
                                        {
                                            infoRow.RelativeItem().Column(col =>
                                            {
                                                col.Item().Text($"Klant: {order.CustomerName ?? "Onbekend"}")
                                                    .FontSize(9);
                                                col.Item().Text($"Email: {order.CustomerEmail ?? "N/A"}")
                                                    .FontSize(8)
                                                    .FontColor(Colors.Grey.Darken1);
                                            });

                                            infoRow.RelativeItem().AlignRight().Text($"€ {order.TotalAmount:N2}")
                                                .FontSize(12)
                                                .Bold()
                                                .FontColor(Colors.Green.Darken1);
                                        });

                                        // Items Table
                                        if (order.Items != null && order.Items.Any())
                                        {
                                            orderCol.Item().PaddingTop(5).Table(table =>
                                            {
                                                table.ColumnsDefinition(columns =>
                                                {
                                                    columns.RelativeColumn(3);
                                                    columns.ConstantColumn(40);
                                                    columns.ConstantColumn(60);
                                                    columns.ConstantColumn(60);
                                                });

                                                // Header
                                                table.Header(header =>
                                                {
                                                    header.Cell().Background(Colors.Grey.Lighten3)
                                                        .Padding(3).Text("Artikel").FontSize(8).Bold();
                                                    header.Cell().Background(Colors.Grey.Lighten3)
                                                        .Padding(3).Text("Aantal").FontSize(8).Bold();
                                                    header.Cell().Background(Colors.Grey.Lighten3)
                                                        .Padding(3).Text("Prijs").FontSize(8).Bold();
                                                    header.Cell().Background(Colors.Grey.Lighten3)
                                                        .Padding(3).Text("Subtotaal").FontSize(8).Bold();
                                                });

                                                // Rows
                                                foreach (var item in order.Items)
                                                {
                                                    table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2)
                                                        .Padding(3).Text(item.BookTitle ?? "Onbekend").FontSize(8);
                                                    
                                                    table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2)
                                                        .Padding(3).AlignCenter().Text($"{item.Quantity}×").FontSize(8);
                                                    
                                                    table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2)
                                                        .Padding(3).AlignRight().Text($"€{item.UnitPrice:N2}").FontSize(8);
                                                    
                                                    table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2)
                                                        .Padding(3).AlignRight().Text($"€{(item.UnitPrice * item.Quantity):N2}").FontSize(8).Bold();
                                                }

                                                // Total Row
                                                table.Cell().ColumnSpan(3).Background(Colors.Grey.Lighten4)
                                                    .Padding(3).AlignRight().Text("TOTAAL:").FontSize(9).Bold();
                                                
                                                table.Cell().Background(Colors.Grey.Lighten4)
                                                    .Padding(3).AlignRight().Text($"€{order.TotalAmount:N2}").FontSize(9).Bold();
                                            });
                                        }
                                    });
                            }
                        });

                    // Footer
                    page.Footer()
                        .BorderTop(1)
                        .BorderColor(Colors.Grey.Lighten1)
                        .PaddingTop(5)
                        .Row(row =>
                        {
                            row.RelativeItem().Text("BestelAppBoeken © 2026")
                                .FontSize(8)
                                .FontColor(Colors.Grey.Darken1);

                            row.RelativeItem().AlignRight().Text("Vertrouwelijk Document")
                                .FontSize(8)
                                .FontColor(Colors.Grey.Darken1);
                        });
                });
            });

            return document.GeneratePdf();
        }

        public byte[] GenerateInventoryPdf(List<BestelAppBoeken.Core.Models.Book> books)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(1.5f, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                    page.Header()
                        .BorderBottom(2)
                        .BorderColor(Colors.Grey.Darken2)
                        .PaddingBottom(10)
                        .Row(row =>
                        {
                            row.RelativeItem().Column(col =>
                            {
                                col.Item().Text("BestelAppBoeken - Voorraadoverzicht")
                                    .FontSize(16)
                                    .Bold()
                                    .FontColor(Colors.Blue.Darken2);

                                col.Item().Text($"Datum: {DateTime.Now:dd-MM-yyyy}")
                                    .FontSize(9);
                            });
                        });

                    page.Content().PaddingVertical(10).Column(column =>
                    {
                        column.Item().Text($"Totaal artikelen: {books.Count}").FontSize(10).Bold();

                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(90);
                                columns.RelativeColumn();
                                columns.ConstantColumn(120);
                                columns.ConstantColumn(60);
                                columns.ConstantColumn(60);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("ISBN").Bold().FontSize(9);
                                header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Titel").Bold().FontSize(9);
                                header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Auteur").Bold().FontSize(9);
                                header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Prijs").Bold().FontSize(9).AlignRight();
                                header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Voorraad").Bold().FontSize(9).AlignRight();
                            });

                            foreach (var b in books)
                            {
                                table.Cell().Padding(4).Text(b.Isbn ?? string.Empty).FontSize(9);
                                table.Cell().Padding(4).Text(b.Title ?? string.Empty).FontSize(9);
                                table.Cell().Padding(4).Text(b.Author ?? string.Empty).FontSize(9);
                                table.Cell().Padding(4).AlignRight().Text($"€{b.Price:N2}").FontSize(9);
                                table.Cell().Padding(4).AlignRight().Text(b.VoorraadAantal.ToString()).FontSize(9);
                            }
                        });
                    });

                    page.Footer().BorderTop(1).BorderColor(Colors.Grey.Lighten1).PaddingTop(5).Row(row =>
                    {
                        row.RelativeItem().Text("BestelAppBoeken © " + DateTime.Now.Year).FontSize(8).FontColor(Colors.Grey.Darken1);
                    });
                });
            });

            return document.GeneratePdf();
        }

        public byte[] GenerateInvoicePdf(Order order)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(1.5f, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                    page.Header().Row(row =>
                    {
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text("BestelAppBoeken")
                                .FontSize(18).Bold().FontColor(Colors.Blue.Darken2);
                            col.Item().Text("Nijverheidskaai 170, 1070 Brussel").FontSize(9);
                        });

                        row.RelativeItem().AlignRight().Column(col =>
                        {
                            col.Item().Text($"Factuur: INV-{order.Id}").FontSize(12).Bold();
                            col.Item().Text($"Datum: {order.OrderDate:dd-MM-yyyy}").FontSize(9);
                        });
                    });

                    page.Content().PaddingVertical(10).Column(col =>
                    {
                        col.Item().Row(r =>
                        {
                            r.RelativeItem().Column(c =>
                            {
                                c.Item().Text("Factuur aan:").FontSize(9).Bold();
                                c.Item().Text(order.CustomerName ?? "Onbekend").FontSize(9);
                                c.Item().Text(order.CustomerEmail ?? "").FontSize(8).FontColor(Colors.Grey.Darken1);
                            });

                            r.ConstantItem(200).Column(c =>
                            {
                                c.Item().Text("Betalingsgegevens").FontSize(9).Bold();
                                c.Item().Text("IBAN: BE12 3456 7890 1234").FontSize(8);
                            });
                        });

                        if (order.Items != null && order.Items.Any())
                        {
                            col.Item().PaddingTop(10).Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(3);
                                    columns.ConstantColumn(50);
                                    columns.ConstantColumn(80);
                                    columns.ConstantColumn(80);
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Background(Colors.Grey.Lighten3).Padding(3).Text("Artikel").FontSize(9).Bold();
                                    header.Cell().Background(Colors.Grey.Lighten3).Padding(3).Text("Aantal").FontSize(9).Bold();
                                    header.Cell().Background(Colors.Grey.Lighten3).Padding(3).Text("Prijs").FontSize(9).Bold().AlignRight();
                                    header.Cell().Background(Colors.Grey.Lighten3).Padding(3).Text("Subtotaal").FontSize(9).Bold().AlignRight();
                                });

                                foreach (var item in order.Items)
                                {
                                    table.Cell().Padding(4).Text(item.BookTitle ?? "").FontSize(9);
                                    table.Cell().Padding(4).AlignCenter().Text(item.Quantity.ToString()).FontSize(9);
                                    table.Cell().Padding(4).AlignRight().Text($"€{item.UnitPrice:N2}").FontSize(9);
                                    table.Cell().Padding(4).AlignRight().Text($"€{(item.UnitPrice * item.Quantity):N2}").FontSize(9).Bold();
                                }

                                table.Cell().ColumnSpan(3).Background(Colors.Grey.Lighten4).Padding(4).AlignRight().Text("TOTAAL:").FontSize(10).Bold();
                                table.Cell().Background(Colors.Grey.Lighten4).Padding(4).AlignRight().Text($"€{order.TotalAmount:N2}").FontSize(10).Bold();
                            });
                        }
                    });

                    page.Footer().BorderTop(1).BorderColor(Colors.Grey.Lighten1).PaddingTop(5).Row(row =>
                    {
                        row.RelativeItem().Text("Bedankt voor uw bestelling!").FontSize(8).FontColor(Colors.Grey.Darken1);
                        row.RelativeItem().AlignRight().Text($"Factuur gegenereerd op {DateTime.Now:dd-MM-yyyy}").FontSize(8).FontColor(Colors.Grey.Darken1);
                    });
                });
            });

            return document.GeneratePdf();
        }
    }
}

using HandlebarsDotNet;
using HtmlToPdf;
using PuppeteerSharp;
using PuppeteerSharp.Media;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<InvoiceFactory>();

Handlebars.RegisterHelper("formatDate", (context, arguments) =>
{
    if (arguments[0] is DateOnly date)
    {
        return date.ToString("dd/MM/yyyy");
    }
    return arguments[0]?.ToString() ?? string.Empty;
});

Handlebars.RegisterHelper("formatCurrency", (context, arguments) =>
{
    if (arguments[0] is decimal value)
    {
        return value.ToString("C", CultureInfo.CreateSpecificCulture("en-US"));
    }
    return arguments[0]?.ToString() ?? string.Empty;
});

Handlebars.RegisterHelper("formatNumber", (context, arguments) =>
{
    if (arguments[0] is decimal value)
    {
        return value.ToString("N2");
    }
    return arguments[0]?.ToString() ?? string.Empty;
});

Handlebars.RegisterHelper("multiply", (context, arguments) =>
{
    if (arguments.Length >= 2 && arguments[0] is decimal a && arguments[1] is decimal b)
    {
        return a * b;
    }
    return 0m;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("invoice-report", async (InvoiceFactory invoiceFactory) =>
{
    var invoice = invoiceFactory.Create(100);

    var templatePath = Path.Combine(Directory.GetCurrentDirectory(), "Views", "InvoiceReport.hbs");
    var templateContent = await File.ReadAllTextAsync(templatePath);

    var template = Handlebars.Compile(templateContent);

    var logoPath = Path.Combine(Directory.GetCurrentDirectory(), "logo.png");
    var logoBytes = await File.ReadAllBytesAsync(logoPath);
    var logoBase64 = Convert.ToBase64String(logoBytes);

    var data = new
    {
        invoice.Number,
        invoice.IssuedDate,
        invoice.DueDate,
        invoice.SellerAddress,
        invoice.CustomerAddress,
        invoice.LineItems,
        Subtotal = invoice.LineItems.Sum(li => li.Price * li.Quantity),
        Total = invoice.LineItems.Sum(li => li.Price * li.Quantity), // optionally add tax or discounts here
        LogoBase64 = logoBase64
    };

    var html = template(data);

    var browserFetcher = new BrowserFetcher();
    await browserFetcher.DownloadAsync();

    using var browser = await Puppeteer.LaunchAsync(new LaunchOptions
    {
        Headless = true,
        Args = new[] { "--no-sandbox", "--disable-setuid-sandbox" }
    });

    using var page = await browser.NewPageAsync();

    await page.SetContentAsync(html);

    await page.EvaluateExpressionHandleAsync("document.fonts.ready");

    var pdfData = await page.PdfDataAsync(new PdfOptions
    {
        HeaderTemplate =
            """
            <div style='font-size: 14px; text-align: center; padding: 10px;'>
                <span style='margin-right: 20px;'><span class='title'></span></span>
                <span><span class='date'></span></span>
            </div>
            """,
        FooterTemplate =
            """
            <div style='font-size: 14px; text-align: center; padding: 10px;'>
                <span style='margin-right: 20px;'>Generated on <span class='date'></span></span>
                <span>Page <span class='pageNumber'></span> of <span class='totalPages'></span></span>
            </div>
            """,
        DisplayHeaderFooter = true,
        Format = PaperFormat.A4,
        PrintBackground = true,
        MarginOptions = new MarginOptions
        {
            Top = "50px",
            Right = "20px",
            Bottom = "50px",
            Left = "20px"
        }
    });

    return Results.File(pdfData, "application/pdf", $"invoice-{invoice.Number}.pdf");
});

app.UseHttpsRedirection();

app.Run();

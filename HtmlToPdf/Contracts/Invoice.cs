namespace HtmlToPdf.Contracts;

public class Invoice
{
    public string Number { get; set; } = string.Empty;
    public DateOnly IssuedDate { get; set; }
    public DateOnly DueDate { get; set; }
    public Address SellerAddress { get; set; } = new();
    public Address CustomerAddress { get; set; } = new();
    public LineItem[] LineItems { get; set; } = [];
}
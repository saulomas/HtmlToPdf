namespace HtmlToPdf.Contracts;

public class LineItem
{
    public int Id { get; set; }
    public string Name { get; set; }
    public decimal Price { get; set; }
    public decimal Quantity { get; set; }
}

using Spectre.Console;

var order = new
{
    OrderId = "PO-2026-001",
    Date = "2026-04-10",
    Customer = new
    {
        Name = "Acme Corp",
        Email = "purchasing@acme.com",
        Address = new
        {
            Street = "123 Main St",
            City = "Springfield",
            State = "IL",
            Zip = "62704"
        }
    },
    Items = new[]
    {
        new { Sku = "WIDGET-01", Description = "Standard Widget", Quantity = 100, UnitPrice = 9.99 },
        new { Sku = "GADGET-05", Description = "Premium Gadget", Quantity = 25, UnitPrice = 49.95 },
        new { Sku = "GIZMO-12", Description = "Deluxe Gizmo", Quantity = 10, UnitPrice = 149.00 },
    },
    Shipping = new
    {
        Method = "Express",
        TrackingNumber = (string?)null,
        Expedited = true
    },
    Notes = "Deliver before end of Q2"
};

var yaml = new YamlText(order);

AnsiConsole.Write(
    new Panel(yaml)
        .Header("Purchase Order")
        .BorderColor(Color.Yellow)
        .Padding(1, 1));


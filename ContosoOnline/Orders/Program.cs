using Orders;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<IOrderService, FakeOrderService>();

var app = builder.Build();

app.MapGet("/orders", async (IOrderService service) =>
    await service.GetOrdersAsync()
    );

app.MapPost("/orders", async (Order order, IOrderService service) =>
    await service.SaveOrderAsync(order)
    );

app.Run();

public record CartItem(string productId, int quantity = 1, decimal pricePerItem = 0)
{
}

public record Order(CartItem[] cart, DateTime orderedAt)
{
}

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Kích hoạt phục vụ file tĩnh
app.UseStaticFiles();
app.UseDefaultFiles();

// Chạy ứng dụng
app.Run();

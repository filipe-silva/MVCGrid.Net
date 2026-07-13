using MVCGrid.AspNetCore;
using MVCGrid.Example.Common;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddMVCGrid(o =>
{
    o.HandlerPath = "/mvcgrid";
    o.ShowErrorDetails = true; // sample app: surface errors to the client
});

var app = builder.Build();

// The shared portable grid catalog registers into the static table (same as the classic host).
SampleGrids.RegisterAll();

app.MapMVCGrid();
app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

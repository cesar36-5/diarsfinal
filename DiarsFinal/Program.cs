using DiarsFinal.Controllers;
using DiarsFinal.Data;
using DiarsFinal.Datos;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

string connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<DiarsContext>(options =>
        options.UseSqlServer(connectionString)
    );

builder.Services.AddScoped<ProductoData>();
builder.Services.AddScoped<PedidoData>();
builder.Services.AddScoped<EspecificacionesData>();
builder.Services.AddScoped<CategoriaData>();
builder.Services.AddScoped<PersonalizadoData>();
builder.Services.AddScoped<IPasarelaPago, PagoYape>();
builder.Services.AddScoped<IPasarelaPago, PagoTarjeta>();
builder.Services.AddScoped<IPasarelaPago, PagoEfectivo>();
builder.Services.AddScoped<DashboardData>();
builder.Services.AddScoped<LoginData>();
builder.Services.AddSession();
builder.Services.AddControllersWithViews();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseSession();

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();
app.UseSession();

app.MapStaticAssets();

app.UseStaticFiles();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


await app.RunAsync();
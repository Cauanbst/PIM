using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Hubs;

var builder = WebApplication.CreateBuilder(args);

// ✅ DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));



// ✅ MVC
builder.Services.AddControllersWithViews();

// ✅ HttpClient para Ollama
builder.Services.AddHttpClient();

// ✅ SignalR
builder.Services.AddSignalR();

// ✅ Session
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// ✅ CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// ✅ Ativar CORS
app.UseCors("AllowAll");

app.UseSession();
app.UseAuthorization();

// ✅ Rotas MVC
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=User}/{action=Login}/{id?}");

// ✅ Rotas da API
app.MapControllers();

// ✅ SignalR Hub
app.MapHub<ChatHub>("/chatHub");

app.Run();

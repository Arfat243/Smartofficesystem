var builder = WebApplication.CreateBuilder(args);

// Session aur HttpContext services add karein
builder.Services.AddControllersWithViews();
builder.Services.AddSession(options => {
    options.IdleTimeout = TimeSpan.FromMinutes(30); // 30 min tak login rahega
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});
builder.Services.AddHttpContextAccessor();

// Database Handler register karein
builder.Services.AddScoped<SmartOfficeSystem.Data.ApplicationDbHandler>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseStaticFiles();
app.UseRouting();

// SESSION MIDDLEWARE (Ye UseRouting ke niche hi hona chahiye)
app.UseSession(); 

app.UseAuthorization();

// Login page ko default banayein
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

app.Run();
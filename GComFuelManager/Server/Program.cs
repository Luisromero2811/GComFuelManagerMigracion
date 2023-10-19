using GComFuelManager.Server;
using GComFuelManager.Server.Helpers;
using GComFuelManager.Server.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.IdentityModel.Tokens;
using RazorHtmlEmails.Common;
using RazorHtmlEmails.GComFuelManagerMigracion.Services;
using System.Globalization;
using System.Text;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDbContext<ApplicationDbContext>(optionsAction =>
{
    optionsAction.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
});

builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
        options.JsonSerializerOptions.PropertyNamingPolicy = null;
    });
builder.Services.AddRazorPages();

builder.Services.AddIdentity<IdentityUsuario, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["jwtkey"]!)),
        ClockSkew = TimeSpan.Zero
    });

builder.Services.AddScoped<IRazorViewToStringRenderer, RazorViewToStringRender>();
builder.Services.AddScoped<IRegisterAccountService, RegisterAccountService>();
builder.Services.AddScoped<IVencimientoService, VencimientoEmailService>();
builder.Services.AddScoped<IPreciosService, PreciosService>();
builder.Services.AddScoped<IConfirmOrden, ConfirmOrden>();
builder.Services.AddSingleton<RequestToFile>();
builder.Services.AddSingleton<VerifyUserToken>();
builder.Services.AddSingleton<VerifyUserId>();
builder.Services.AddSingleton(new CultureInfo("es-Mx"));

builder.Services.Configure<CookiePolicyOptions>(options =>
{
    options.HttpOnly = Microsoft.AspNetCore.CookiePolicy.HttpOnlyPolicy.Always;
    options.Secure = CookieSecurePolicy.Always;
   
});

var app = builder.Build();

using(var scope = app.Services.CreateScope())
{
    var applicationDbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    applicationDbContext.Database.Migrate();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
//Politica CRS para evitar ataques XSS
app.Use(async (context, next) =>
{
    context.Response.Headers.Add("Content-Security-Policy", "default-src 'self'; upgrade-insecure-requests; base-uri 'self'; object-src 'none'; script-src 'self' 'unsafe-eval' 'unsafe-inline'; style-src 'self' 'unsafe-inline'; img-src 'self' blob: https: data:; connect-src 'self'");
    await next();
});

app.UseHttpsRedirection();

app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();
app.MapControllers();
app.MapFallbackToFile("index.html");

app.Run();


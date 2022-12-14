using BlenderParadise.Data;
using BlenderParadise.Data.Models;
using BlenderParadise.Services.Contracts;
using BlenderParadise.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using BlenderParadise.Repositories.Contracts;
using BlenderParadise.Repositories;
using Quartz;
using Microsoft.AspNetCore.Mvc;
using BlenderParadise.Infrastucture;
using BlenderParadise.Areas.Admin.Services.Contracts;
using BlenderParadise.Areas.Admin.Services;
using BlenderParadise.Services.Jobs;
using BlenderParadise.Services.Services.FileServices;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("AzureConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = false)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add<AutoValidateAntiforgeryTokenAttribute>();
});

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/User/Login";
    options.AccessDeniedPath = "/Home/Index";
});
builder.Services.AddScoped<IUploadService, UploadService>();
builder.Services.AddScoped<IDownloadService, DownloadService>();
builder.Services.AddScoped<IProfileService, ProfileService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IControlService, ControlService>();
builder.Services.AddScoped<IChallengeService, ChallengeService>();
builder.Services.AddScoped<IRepository, Repository>();
builder.Services.AddScoped<IFileService>(_ =>
    new AzureFileService(builder.Configuration.GetConnectionString("BlobStorageConnection")));

builder.Services.AddQuartz(q =>
{
    q.SchedulerId = "Scheduler-Core";

    q.UseMicrosoftDependencyInjectionJobFactory();
    q.UseSimpleTypeLoader();
    q.UseInMemoryStore();
    q.UseDefaultThreadPool(tp =>
    {
        tp.MaxConcurrency = 10;
    });

    q.ScheduleJob<ChallengeJob>(trigger => trigger
            .WithIdentity("Combined Configuration Trigger")
            .StartNow()
            .WithDailyTimeIntervalSchedule(x => x.StartingDailyAt(TimeOfDay.HourAndMinuteOfDay(0, 0)).OnEveryDay())
        );
});

builder.Services.AddQuartzHostedService(options =>
{
    options.WaitForJobsToComplete = true;
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
    app.UseExceptionHandler("/Home/Error");
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.SeedAdmin();

app.UseEndpoints(endpoints =>
{
    endpoints.MapControllerRoute(
        name: "areas",
        pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}"
    );

    endpoints.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}"
    );

    endpoints.MapDefaultControllerRoute();

    endpoints.MapRazorPages();
});

app.Run();

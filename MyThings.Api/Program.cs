using Microsoft.EntityFrameworkCore;
using MyThings.Api.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// EF Core: connect MyThingsDbContext to SQL Server.
// "Default" comes from appsettings.Development.json, the password from user-secrets.
builder.Services.AddDbContext<MyThingsDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

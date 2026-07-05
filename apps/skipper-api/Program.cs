using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using skipper_api.Data;
using skipper_api.Services.Enclosures;

if (Environment.GetEnvironmentVariable("CRITTEROPS_EF_DESIGN_TIME") == "true")
{
    return;
}

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Database — EF Core with Npgsql (PostgreSQL)
builder.Services.AddDbContext<ProfessorDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("ProfessorDb")));

builder.Services.AddScoped<IEnclosureService, EnclosureService>();

// Health checks — DB check is tagged "db" so it can be filtered independently
builder.Services.AddHealthChecks()
    .AddDbContextCheck<ProfessorDbContext>(name: "professor-db", tags: ["db"]);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

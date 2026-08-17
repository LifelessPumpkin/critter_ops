using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using skipper_api.Data;
using skipper_api.Services.Activity;
using skipper_api.Services.Animals;
using skipper_api.Services.AnimalDispositions;
using skipper_api.Services.AnimalFeedings;
using skipper_api.Services.AnimalMedications;
using skipper_api.Services.AnimalMovements;
using skipper_api.Services.AnimalTimeline;
using skipper_api.Services.AnimalTreatments;
using skipper_api.Services.EnclosureCleanings;
using skipper_api.Services.Enclosures;
using skipper_api.Services.EnclosureTimeline;

if (Environment.GetEnvironmentVariable("CRITTEROPS_EF_DESIGN_TIME") == "true")
{
    return;
}

var builder = WebApplication.CreateBuilder(args);
const string GilliganWebCorsPolicy = "GilliganWeb";

// Add services to the container.

builder.Services.AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? [];

builder.Services.AddCors(options =>
{
    options.AddPolicy(GilliganWebCorsPolicy, policy =>
    {
        policy
            .WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// Database — EF Core with Npgsql (PostgreSQL)
builder.Services.AddDbContext<ProfessorDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("ProfessorDb")));

builder.Services.AddScoped<IEnclosureService, EnclosureService>();
builder.Services.AddScoped<IActivitySearchService, ActivitySearchService>();
builder.Services.AddScoped<IAnimalService, AnimalService>();
builder.Services.AddScoped<IAnimalDispositionActivityService, AnimalDispositionActivityService>();
builder.Services.AddScoped<IAnimalFeedingActivityService, AnimalFeedingActivityService>();
builder.Services.AddScoped<IAnimalMedicationActivityService, AnimalMedicationActivityService>();
builder.Services.AddScoped<IAnimalMovementActivityService, AnimalMovementActivityService>();
builder.Services.AddScoped<IAnimalTimelineService, AnimalTimelineService>();
builder.Services.AddScoped<IAnimalTreatmentActivityService, AnimalTreatmentActivityService>();
builder.Services.AddScoped<IEnclosureCleaningActivityService, EnclosureCleaningActivityService>();
builder.Services.AddScoped<IEnclosureTimelineService, EnclosureTimelineService>();

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

app.UseCors(GilliganWebCorsPolicy);

app.UseAuthorization();

app.MapControllers();

app.Run();

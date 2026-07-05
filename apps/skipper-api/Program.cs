using Microsoft.EntityFrameworkCore;
using skipper_api.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Database — EF Core with Npgsql (PostgreSQL)
builder.Services.AddDbContext<ProfessorDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("ProfessorDb")));

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


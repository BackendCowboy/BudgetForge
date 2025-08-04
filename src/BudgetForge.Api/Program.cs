var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers(); // This is what was missing!
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Map controllers - this is also what was missing!
app.MapControllers();

app.Run();
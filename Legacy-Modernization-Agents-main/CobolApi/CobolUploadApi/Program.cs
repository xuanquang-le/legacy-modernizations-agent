using CobolUploadApi.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { 
        Title = "COBOL Upload API with Neo4j", 
        Description = "API for uploading and analyzing COBOL files with Neo4j storage",
        Version = "v1" 
    });
});

// Register services
builder.Services.AddSingleton<INeo4jService, Neo4jService>();
builder.Services.AddSingleton<ICobolStorageService, CobolStorageService>();

// CORS
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

// Khởi tạo Neo4j
var neo4jService = app.Services.GetRequiredService<INeo4jService>();
await neo4jService.InitializeAsync();

// Configure pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthorization();
app.MapControllers();

// Tạo thư mục storage
var storagePath = Path.Combine(app.Environment.ContentRootPath, "Storage", "CobolFiles");
Directory.CreateDirectory(storagePath);

app.Run();
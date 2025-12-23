using QuestPDF.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

QuestPDF.Settings.License = LicenseType.Community;

builder.Services.AddControllers();
builder.Services.AddScoped<DapperContext>();
builder.Services.AddScoped<ISaleOutService, SaleOutService>();
builder.Services.AddScoped<IMasterProductService, MasterProductService>();
builder.Services.AddScoped<ITemplateFileService, TemplateFileService>();
builder.Services.AddScoped<IReportPdfService, ReportPdfService>();
builder.Services.AddScoped<ValidationUploadFileSaleOut>();
builder.Services.AddScoped<ValidationUploadFileMasterProduct>();


var MyAllowSpecificOrigins = "_myAllowSpecificOrigins";

builder.Services.AddCors(options =>
{
    options.AddPolicy(name: MyAllowSpecificOrigins,
        policy =>
        {
            policy.WithOrigins("http://localhost:5173")
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
});
var app = builder.Build();
app.UseMiddleware<ExceptionMiddleware>();

app.UseRouting();
app.UseHttpsRedirection();
app.UseCors(MyAllowSpecificOrigins);
app.UseAuthorization();
app.MapControllers();

app.Run();
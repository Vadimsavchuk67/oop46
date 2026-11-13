using Microsoft.EntityFrameworkCore;
using PostService.DataAccess;
using PostService.Dtos;
using PostService.Mappings;
using PostService.BusinessLogic;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddScoped<IPostingService, PostingService>();
builder.Services.AddScoped<IPostingRepository, PostingRepository>();
builder.Services.AddDbContext<PostingDbContext>();
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});
var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<PostingDbContext>();
    context.Database.EnsureCreated();
}

app.MapGet("/", () => "Це API поштового клієнта.");

app.MapGet("/postings", (IPostingService service) =>
{
    if (app.Environment.IsDevelopment() && app.Configuration.GetValue<bool>("TestErrors"))
        throw new InvalidOperationException("Перевірка глобального обробника.");
    var postings = service.GetAll();
    return Results.Ok(postings.Select(PostingMapper.ToPostingGetDto).ToList());
});

app.MapGet("/postings/{id:int}", (int id, IPostingService service) =>
{
    var posting = service.Find(id);
    if (posting == null)
        return Results.NotFound();

    return Results.Ok(PostingMapper.ToPostingGetDto(posting));
});

app.MapPost("/postings", (PostingPostDto dto, IPostingService service) =>
{
    var posting = PostingMapper.ToPosting(dto);
    var errors = Validate(posting);
    if (errors.Count > 0) return Results.ValidationProblem(errors);
    var saved = service.Create(posting);
    var result = PostingMapper.ToPostingGetDto(saved);
    return Results.Created($"/postings/{result.Id}", result);
});

app.MapPut("/postings/{id:int}", (int id, PostingPutDto dto, IPostingService service) =>
{
    if (id != dto.Id)
        return Results.ValidationProblem(new Dictionary<string, string[]> { ["Id"] = new[] { "Номер у запиті не збігається з номером відправлення." } });

    var posting = PostingMapper.ToPosting(dto);
    var errors = Validate(posting);
    if (!float.IsFinite(posting.Price) || posting.Price < 0) errors["Price"] = new[] { "Вартість не може бути від’ємною." };
    if (errors.Count > 0) return Results.ValidationProblem(errors);
    var updated = service.Update(posting);
    if (updated == null)
        return Results.NotFound();

    return Results.Ok(PostingMapper.ToPostingGetDto(updated));
});

app.MapDelete("/postings/{id:int}", (int id, IPostingService service) =>
{
    var result = service.Delete(id);
    if (result == 0)
        return Results.NotFound();

    return Results.NoContent();
});

app.Run();

static Dictionary<string, string[]> Validate(PostService.Models.Posting p)
{
    var errors = new Dictionary<string, string[]>();
    if (string.IsNullOrWhiteSpace(p.From)) errors["From"] = new[] { "Вкажіть відправника." };
    if (string.IsNullOrWhiteSpace(p.To)) errors["To"] = new[] { "Вкажіть отримувача." };
    if (string.IsNullOrWhiteSpace(p.Content)) errors["Content"] = new[] { "Вкажіть вміст." };
    if (!Enum.IsDefined(p.DeliveryType)) errors["DeliveryType"] = new[] { "Невідомий тип доставки." };
    if (!float.IsFinite(p.Weight) || p.Weight <= 0) errors["Weight"] = new[] { "Вага має бути більшою за 0." };
    if (p.Width <= 0 || p.Height <= 0 || p.Depth <= 0) errors["Size"] = new[] { "Розміри мають бути більшими за 0." };
    if (!float.IsFinite(p.Value) || p.Value < 0) errors["Value"] = new[] { "Цінність не може бути від’ємною." };
    return errors;
}

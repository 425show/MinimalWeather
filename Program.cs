var builder = WebApplication.CreateBuilder(args);
builder.Services.AddCors(options => options.AddPolicy("allowAny", o => o.AllowAnyOrigin()));
builder.Services.AddMicrosoftIdentityWebApiAuthentication(builder.Configuration);
builder.Services.AddAuthorization();

var app = builder.Build();

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/secure", [EnableCors("allowAny")] () =>
{
    //http.
    return "hello from secure";
}).RequireAuthorization();

app.MapGet("/insecure", [EnableCors("allowAny")] () =>
{
    return "hello from insecure";
});

app.Run();
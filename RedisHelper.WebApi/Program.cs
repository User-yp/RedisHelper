using Com.Ctrip.Framework.Apollo.Enums;
using Com.Ctrip.Framework.Apollo;
using RedisHelper;
using Newtonsoft.Json;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.WebHost.ConfigureAppConfiguration((host, cfg) =>
{
    cfg.AddApollo(host.Configuration.GetSection("apollo"))
    .AddNamespace("User.CommonConfiguration", ConfigFileFormat.Json).AddDefault(); 
});

builder.Services.AddRedisHelper(redisOption =>
{
    //Get ConnectionString from apollo
    redisOption.ConnectionString = builder.Configuration.GetSection("Redis").Value;
    redisOption.DbNumber = 1;
});


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

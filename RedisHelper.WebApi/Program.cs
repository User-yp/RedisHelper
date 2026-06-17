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

var str = builder.Configuration.GetSection("RedisConnStr").Get<string>() ?? "127.0.0.1:6379";
builder.Services.AddRedisHelper(str, 2);



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

using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using AnthemAPI.Services;
using Quartz;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpContextAccessor();

builder.Services.AddSingleton<IAmazonDynamoDB>(sp =>
    {
        string awsAccessKeyId = builder.Configuration["AWS_ACCESS_KEY_ID"]!;
        string awsSecretAccessKey = builder.Configuration["AWS_SECRET_ACCESS_KEY"]!;
        string awsSessionToken = builder.Configuration["AWS_SESSION_TOKEN"]!;

        return new AmazonDynamoDBClient(awsAccessKeyId, awsSecretAccessKey, awsSessionToken, RegionEndpoint.USEast1);
    }
);

builder.Services.AddScoped<DynamoDBContext>();
builder.Services.AddScoped<JobService>();
builder.Services.AddScoped<StatusConnectionService>();
builder.Services.AddScoped<UserService>();

builder.Services.AddHttpClient<AuthorizationService>();
builder.Services.AddHttpClient<SpotifyService>();

builder.Services.AddQuartz();
builder.Services.AddQuartzHostedService();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers();
app.Run();

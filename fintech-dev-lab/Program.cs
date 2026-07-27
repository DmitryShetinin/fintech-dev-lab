using Application.Abstractions.Persistence;
using Application.Abstractions.Providers;
using Application.Interface;
using Application.Operations;
using Application.Receipts;
using Core.Models;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Repositories;
using Infrastructure.Providers;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);



builder.Services.AddSingleton<OperationStateMachine>();

builder.Services.AddScoped<IOperationService, OperationService>();
builder.Services.AddScoped<IReceiptService, ReceiptService>();


// MVC
builder.Services.AddControllers();


// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


// Database
builder.Services.AddDbContext<AppDbContext>(
    options =>
    {
      options.UseNpgsql(
          builder.Configuration.GetConnectionString("Default"));
    });


// Application services

builder.Services.AddSingleton<OperationStateMachine>();


builder.Services.AddScoped<IOperationService, OperationService>();

builder.Services.AddScoped<IReceiptService, ReceiptService>();


// Repositories

builder.Services.AddScoped<IOperationRepository, OperationRepository>();

// builder.Services.AddScoped<IPaymentAttemptRepository, PaymentAttemptRepository>();


// Unit Of Work

builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();


// Providers

builder.Services.AddHttpClient<IProviderClient, HttpProviderClient>();



var app = builder.Build();



app.UseSwagger();
app.UseSwaggerUI();


app.MapControllers();


app.Run();

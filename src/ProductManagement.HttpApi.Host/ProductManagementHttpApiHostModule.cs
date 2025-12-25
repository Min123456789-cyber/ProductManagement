using Medallion.Threading;
using Medallion.Threading.Redis;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Models;
using ProductManagement.ApiKeyAuthentication;
using ProductManagement.Constants;
using ProductManagement.EntityFrameworkCore;
using ProductManagement.MultiTenancy;
using ProductManagement.RateLimiting;
using ProductManagement.Options;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Volo.Abp;
using Volo.Abp.AspNetCore.Authentication.JwtBearer;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.MultiTenancy;
using Volo.Abp.AspNetCore.Serilog;
using Volo.Abp.Autofac;
using Volo.Abp.Caching;
using Volo.Abp.Caching.StackExchangeRedis;
using Volo.Abp.DistributedLocking;
using Volo.Abp.Modularity;
using Volo.Abp.Security.Claims;
using Volo.Abp.Swashbuckle;
using Volo.Abp.VirtualFileSystem;

namespace ProductManagement;

[DependsOn(
    typeof(ProductManagementHttpApiModule),
    typeof(AbpAutofacModule),
    typeof(AbpCachingStackExchangeRedisModule),
    typeof(AbpDistributedLockingModule),
    typeof(AbpAspNetCoreMvcUiMultiTenancyModule),
    typeof(AbpAspNetCoreAuthenticationJwtBearerModule),
    typeof(ProductManagementApplicationModule),
    typeof(ProductManagementEntityFrameworkCoreModule),
    typeof(AbpAspNetCoreSerilogModule),
    typeof(AbpSwashbuckleModule)
)]
public class ProductManagementHttpApiHostModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();
        var hostingEnvironment = context.Services.GetHostingEnvironment();

        context.Services.AddHttpClient();

        ConfigureConventionalControllers();
        ConfigureAuthentication(context, configuration);
        ConfigureCache(configuration);
        ConfigureVirtualFileSystem(context);
        ConfigureDataProtection(context, configuration, hostingEnvironment);
        ConfigureDistributedLocking(context, configuration);
        ConfigureCors(context, configuration);
        ConfigureSwaggerServices(context, configuration);
        ConfigureRateLimiting(context.Services);
        
        // Configure file upload options
        context.Services.Configure<IISServerOptions>(options =>
        {
            options.MaxRequestBodySize = int.MaxValue; // or set a specific limit
        });

        context.Services.Configure<FormOptions>(options =>
        {
            options.ValueLengthLimit = int.MaxValue;
            options.MultipartBodyLengthLimit = int.MaxValue;
            options.MultipartHeadersLengthLimit = int.MaxValue;
            options.BufferBody = true;
        });
    }


    private void ConfigureCache(IConfiguration configuration)
    {
        Configure<AbpDistributedCacheOptions>(options => { options.KeyPrefix = "ProductManagement:"; });
    }

    private void ConfigureVirtualFileSystem(ServiceConfigurationContext context)
    {
        var hostingEnvironment = context.Services.GetHostingEnvironment();

        if (hostingEnvironment.IsDevelopment())
        {
            Configure<AbpVirtualFileSystemOptions>(options =>
            {
                options.FileSets.ReplaceEmbeddedByPhysical<ProductManagementDomainSharedModule>(
                    Path.Combine(hostingEnvironment.ContentRootPath,
                        $"..{Path.DirectorySeparatorChar}ProductManagement.Domain.Shared"));
                options.FileSets.ReplaceEmbeddedByPhysical<ProductManagementDomainModule>(
                    Path.Combine(hostingEnvironment.ContentRootPath,
                        $"..{Path.DirectorySeparatorChar}ProductManagement.Domain"));
                options.FileSets.ReplaceEmbeddedByPhysical<ProductManagementApplicationContractsModule>(
                    Path.Combine(hostingEnvironment.ContentRootPath,
                        $"..{Path.DirectorySeparatorChar}ProductManagement.Application.Contracts"));
                options.FileSets.ReplaceEmbeddedByPhysical<ProductManagementApplicationModule>(
                    Path.Combine(hostingEnvironment.ContentRootPath,
                        $"..{Path.DirectorySeparatorChar}ProductManagement.Application"));
            });
        }
    }

    private void ConfigureConventionalControllers()
    {
        Configure<AbpAspNetCoreMvcOptions>(options =>
        {
            options.ConventionalControllers.Create(typeof(ProductManagementApplicationModule).Assembly);
        });
    }

    private void ConfigureAuthentication(ServiceConfigurationContext context, IConfiguration configuration)
    {
        // First, configure JWT Bearer authentication
        context.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddAbpJwtBearer(options =>
            {
                options.Authority = configuration["AuthServer:Authority"];
                options.RequireHttpsMetadata = configuration.GetValue<bool>("AuthServer:RequireHttpsMetadata");
                options.Audience = "ProductManagement";
            });

        // Now, add API key authentication
        var authBuilder = context.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme);

        authBuilder.AddScheme<ApiKeyAuthenticationOptions, ApiKeyAuthenticationHandler>(
            CustomAuthentication.ApiKeyScheme, // Using the constant ApiKeyScheme defined earlier
            options =>
            {
                // Bind the ApiKey authentication settings from configuration
                configuration.Bind("ApiKeyAuthentication", options);
            });

        context.Services.AddAuthorization(options =>
        {

            options.AddPolicy(CustomAuthentication.ApiKeyOrBearerTokenPolicy, policy =>
            {
                policy.AuthenticationSchemes.Add(CustomAuthentication.ApiKeyScheme);
                policy.AuthenticationSchemes.Add(JwtBearerDefaults.AuthenticationScheme);
                policy.RequireAuthenticatedUser();
            });
        });

        context.Services.Configure<AbpClaimsPrincipalFactoryOptions>(options =>
        {
            options.IsDynamicClaimsEnabled = true;
        });
    }

    private static void ConfigureSwaggerServices(ServiceConfigurationContext context, IConfiguration configuration)
    {
        context.Services.AddAbpSwaggerGenWithOAuth(
            configuration["AuthServer:Authority"]!,
            new Dictionary<string, string>
            {
                    {"ProductManagement", "ProductManagement API"}
            },
            options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo { Title = "ProductManagement API", Version = "v1" });
                options.DocInclusionPredicate((docName, description) => true);
                options.CustomSchemaIds(type => type.FullName);
            });
    }

    private void ConfigureDataProtection(
        ServiceConfigurationContext context,
        IConfiguration configuration,
        IWebHostEnvironment hostingEnvironment)
    {
        var dataProtectionBuilder = context.Services.AddDataProtection().SetApplicationName("ProductManagement");
        if (!hostingEnvironment.IsDevelopment())
        {
            var redis = ConnectionMultiplexer.Connect(configuration["Redis:Configuration"]!);
            dataProtectionBuilder.PersistKeysToStackExchangeRedis(redis, "ProductManagement-Protection-Keys");
        }
    }

    private void ConfigureDistributedLocking(
        ServiceConfigurationContext context,
        IConfiguration configuration)
    {
        context.Services.AddSingleton<IDistributedLockProvider>(sp =>
        {
            var connection = ConnectionMultiplexer.Connect(configuration["Redis:Configuration"]!);
            return new RedisDistributedSynchronizationProvider(connection.GetDatabase());
        });
    }

    private void ConfigureCors(ServiceConfigurationContext context, IConfiguration configuration)
    {
        context.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(builder =>
            {
                builder
                    .WithOrigins(configuration["App:CorsOrigins"]?
                        .Split(",", StringSplitOptions.RemoveEmptyEntries)
                        .Select(o => o.RemovePostFix("/"))
                        .ToArray() ?? Array.Empty<string>())
                    .WithAbpExposedHeaders()
                    .SetIsOriginAllowedToAllowWildcardSubdomains()
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
        });
    }

    private void ConfigureRateLimiting(IServiceCollection services)
    {
        services.AddRateLimiting(options =>
        {
            options.RequestsPerMinute = 5;
            options.WhitelistedIPs.Add("127.0.0.1");
            options.EnableExponentialBackoff = true;
            options.BaseBackoffSeconds = 60;

            // Configure different limits for different endpoints
            options.EndpointLimits.Add("/api/product/create", 2); // More restrictive for create
            options.EndpointLimits.Add("/api/product/update", 2); // More restrictive for update
            options.EndpointLimits.Add("/api/product/delete", 1); // Most restrictive for delete

            // Uncomment to use Redis for distributed rate limiting
            // options.UseRedis = true;
            // options.RedisConnectionString = "localhost:6379";
        });
    }

    public override void OnApplicationInitialization(ApplicationInitializationContext context)
    {
        var app = context.GetApplicationBuilder();
        var env = context.GetEnvironment();

        // Configure request body size limit
        app.Use(async (context, next) =>
        {
            context.Features.Get<IHttpMaxRequestBodySizeFeature>().MaxRequestBodySize = null;
            await next.Invoke();
        });

        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        // Localization and other required middlewares
        app.UseAbpRequestLocalization();  // Handles localization for the application
        app.UseCorrelationId();           // Correlation ID for tracking requests
        app.UseStaticFiles();             // Static file middleware for serving static files (CSS, JS, etc.)
        app.UseRouting();                 // Enable routing for the application

        app.UseCors();                    // Enable Cross-Origin Resource Sharing (CORS)

        app.UseAuthentication();          // Ensure authentication happens before authorization

        if (MultiTenancyConsts.IsEnabled)
        {
            app.UseMultiTenancy();        // Use multi-tenancy if enabled
        }

        app.UseUnitOfWork();              // Ensure that the unit of work pattern is used in requests
        app.UseDynamicClaims();           // Add dynamic claims (if you have dynamic claims set up)
        app.UseAuthorization();           // Authorization happens after authentication

        // Swagger for API documentation
        app.UseSwagger();
        app.UseAbpSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "ProductManagement API");

            var configuration = context.GetConfiguration();
            options.OAuthClientId(configuration["AuthServer:SwaggerClientId"]);
            options.OAuthScopes("ProductManagement");
        });

        // Auditing middleware for logging activities
        app.UseAuditing();
        app.UseAbpSerilogEnrichers();
        app.UseRateLimiting();
        app.UseConfiguredEndpoints();
    }

}

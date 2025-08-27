using Microsoft.Extensions.DependencyInjection;
using ProductManagement.AppServices.Teachers;
using ProductManagement.Export;
using ProductManagement.Export.Formatters;
using ProductManagement.Teachers;
using Volo.Abp.Account;
using Volo.Abp.AutoMapper;
using Volo.Abp.FeatureManagement;
using Volo.Abp.Identity;
using Volo.Abp.Modularity;
using Volo.Abp.PermissionManagement;
using Volo.Abp.SettingManagement;
using Volo.Abp.TenantManagement;

namespace ProductManagement;

[DependsOn(
    typeof(ProductManagementDomainModule),
    typeof(AbpAccountApplicationModule),
    typeof(ProductManagementApplicationContractsModule),
    typeof(AbpIdentityApplicationModule),
    typeof(AbpPermissionManagementApplicationModule),
    typeof(AbpTenantManagementApplicationModule),
    typeof(AbpFeatureManagementApplicationModule),
    typeof(AbpSettingManagementApplicationModule)
    )]
public class ProductManagementApplicationModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpAutoMapperOptions>(options =>
        {
            options.AddMaps<ProductManagementApplicationModule>();
        });

        // Register export formatters
        context.Services.AddTransient(typeof(CsvExportFormatter<>));
        context.Services.AddTransient(typeof(ExcelExportFormatter<>));
        context.Services.AddTransient(typeof(JsonExportFormatter<>));
        context.Services.AddTransient(typeof(XmlExportFormatter<>));

        // Register generic export services
        context.Services.AddTransient(typeof(IExportService<,,>), typeof(GenericExportService<,,>));
        context.Services.AddTransient(typeof(BaseExportService<,,>));

        // Register specific export services
        context.Services.AddTransient<TeacherExportService>();
    }
}

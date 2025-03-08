using Microsoft.Extensions.Localization;
using ProductManagement.Localization;
using Volo.Abp.Ui.Branding;
using Volo.Abp.DependencyInjection;

namespace ProductManagement;

[Dependency(ReplaceServices = true)]
public class ProductManagementBrandingProvider : DefaultBrandingProvider
{
    private IStringLocalizer<ProductManagementResource> _localizer;

    public ProductManagementBrandingProvider(IStringLocalizer<ProductManagementResource> localizer)
    {
        _localizer = localizer;
    }

    public override string AppName => _localizer["AppName"];
}

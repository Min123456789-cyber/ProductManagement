using ProductManagement.Localization;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Localization;

namespace ProductManagement.Permissions
{
    public class ProductManagementPermissionDefinitionProvider : PermissionDefinitionProvider
    {
        public override void Define(IPermissionDefinitionContext context)
        {
            // Add main permission group
            var myGroup = context.AddGroup(ProductManagementPermissions.GroupName, L("Permission:ProductManagement"));

            // Define product-related permissions
            var productGroup = context.AddGroup(ProductManagementPermissions.GroupProduct);
            var productPermission = productGroup.AddPermission(ProductManagementPermissions.Product.Default, L("Permission:Product"));
            productPermission.AddChild(ProductManagementPermissions.Product.Create, L("Permission:Product.Create"));
            productPermission.AddChild(ProductManagementPermissions.Product.Edit, L("Permission:Product.Edit"));
            productPermission.AddChild(ProductManagementPermissions.Product.Delete, L("Permission:Product.Delete"));
            productPermission.AddChild(ProductManagementPermissions.Product.View, L("Permission:Product.View"));

            // Define category-related permissions
            var categoryGroup = context.AddGroup(ProductManagementPermissions.GroupCategory);
            var categoryPermission = categoryGroup.AddPermission(ProductManagementPermissions.Category.Default, L("Permission:Category"));
            categoryPermission.AddChild(ProductManagementPermissions.Category.Create, L("Permission:Category.Create"));
            categoryPermission.AddChild(ProductManagementPermissions.Category.Edit, L("Permission:Category.Edit"));
            categoryPermission.AddChild(ProductManagementPermissions.Category.Delete, L("Permission:Category.Delete"));
            categoryPermission.AddChild(ProductManagementPermissions.Category.View, L("Permission:Category.View"));
        }

        private static LocalizableString L(string name)
        {
            return LocalizableString.Create<ProductManagementResource>(name);
        }
    }
}

namespace ProductManagement.Permissions
{
    public static class ProductManagementPermissions
    {
        public const string GroupName = "ProductManagement";

        public const string GroupProduct = "Product";
        public const string GroupCategory = "Category";

        // Product Permissions
        public static class Product
        {
            public const string Default = GroupProduct;
            public const string Create = Default + ".Create";
            public const string Edit = Default + ".Edit";
            public const string Delete = Default + ".Delete";
            public const string View = Default + ".View";
        }

        // Category Permissions
        public static class Category
        {
            public const string Default = GroupCategory;
            public const string Create = Default + ".Create";
            public const string Edit = Default + ".Edit";
            public const string Delete = Default + ".Delete";
            public const string View = Default + ".View";
        }
    }
}

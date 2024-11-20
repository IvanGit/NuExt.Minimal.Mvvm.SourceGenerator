namespace Minimal.Mvvm
{
    internal enum AccessModifier
    {
        Default = 0,
        Public = 6,
        ProtectedInternal = 5,
        Internal = 4,
        Protected = 3,
        PrivateProtected = 2,
        Private = 1,
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    internal sealed class NotifyAttribute : Attribute
    {
        public NotifyAttribute()
        {

        }

        public NotifyAttribute(string propertyName)
        {

        }

        public string PropertyName { get; set; } = null!;

        public string CallbackName { get; set; } = null!;

        public bool PreferCallbackWithParameter { get; set; }

        public AccessModifier Getter { get; set; }

        public AccessModifier Setter { get; set; }

    }
}

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

    [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
    internal sealed class NotifyAttribute : Attribute
    {
        public string PropertyName { get; set; } = null!;

        public AccessModifier Getter { get; set; }

        public AccessModifier Setter { get; set; }

    }
}

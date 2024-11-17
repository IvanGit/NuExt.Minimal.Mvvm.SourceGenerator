namespace Minimal.Mvvm
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    internal sealed class LocalizeAttribute : Attribute
    {
        public LocalizeAttribute(string jsonFileName)
        {

        }
    }
}

﻿namespace NuExt.Minimal.Mvvm.SourceGenerator.Tests
{
    internal static partial class PropertyNames
    {
        public static List<(string source, string? expected)> Sources =
        [
            (
                source : """
                using Minimal.Mvvm;

                public partial class MyModel : BindableBase
                {
                    [Notify("MyName")]
                    private string _name = null!;

                    [Notify(PropertyName = nameof(MyDescription))]
                    private string? _description;

                    [Notify("MyDescription", PropertyName = nameof(MyDescription2))]
                    private string? _description2;
                }
                """,
                expected : """
                // <auto-generated>
                //     Auto-generated by [GeneratorName] [GeneratorVersion]
                // </auto-generated>

                #nullable enable

                partial class MyModel
                {
                    public string MyName
                    {
                        get => _name;
                        set => SetProperty(ref _name, value);
                    }

                    public string? MyDescription
                    {
                        get => _description;
                        set => SetProperty(ref _description, value);
                    }

                    public string? MyDescription2
                    {
                        get => _description2;
                        set => SetProperty(ref _description2, value);
                    }
                }
                """ ),
        ];
    }
}

# NuExt.Minimal.Mvvm.SourceGenerator

`NuExt.Minimal.Mvvm.SourceGenerator` is an extension for the lightweight MVVM framework [NuExt.Minimal.Mvvm](https://github.com/IvanGit/NuExt.Minimal.Mvvm). This package includes a source generator that produces boilerplate code for your ViewModels at compile time, simplifying development and reducing routine work. By automating repetitive tasks, it helps you focus on implementing application-specific logic.

### Features

- Automatically generates boilerplate code for ViewModels.
- Simplifies the development process by reducing repetitive coding tasks.
- Seamlessly integrates with the NuExt.Minimal.Mvvm framework.
- Enhances maintainability and readability of your codebase.

### Installation

You can install `NuExt.Minimal.Mvvm.SourceGenerator` via [NuGet](https://www.nuget.org/):

```sh
dotnet add package NuExt.Minimal.Mvvm.SourceGenerator
```

Or through the Visual Studio package manager:

1. Go to `Tools -> NuGet Package Manager -> Manage NuGet Packages for Solution...`.
2. Search for `NuExt.Minimal.Mvvm.SourceGenerator`.
3. Click "Install".

### Dependencies

To use this source generator effectively, you need to have any of these packages installed in your project: [`NuExt.Minimal.Mvvm`](https://www.nuget.org/packages/NuExt.Minimal.Mvvm), [`NuExt.Minimal.Mvvm.Windows`](https://www.nuget.org/packages/NuExt.Minimal.Mvvm.Windows), or [`NuExt.Minimal.Mvvm.MahApps.Metro`](https://www.nuget.org/packages/NuExt.Minimal.Mvvm.MahApps.Metro). You can add them via NuGet as well:

For the base MVVM framework:
```sh
dotnet add package NuExt.Minimal.Mvvm
```

For Windows-specific extensions:
```sh
dotnet add package NuExt.Minimal.Mvvm.Windows
```

For MahApps.Metro integration:
```sh
dotnet add package NuExt.Minimal.Mvvm.MahApps.Metro
```

### Usage Examples

#### Example using auto-generated property notifications

Given a user class such as:

```csharp
using Minimal.Mvvm;

public partial class MyModel : BindableBase
{
    [Notify]
    private string? _description;

    [Notify(Setter = AccessModifier.Private)]
    private string _name;
}
```

The generator could produce the following:

```csharp
partial class MyModel
{
    public string Description
    {
        get => _description;
        set => SetProperty(ref _description, value);
    }

    public string Name
    {
        get => _name;
        private set => SetProperty(ref _name, value);
    }
}
```

This example demonstrates how the source generator automatically creates properties with notification changes for fields marked with the `[Notify]` attribute, thereby reducing boilerplate code.

### Contributing

Contributions are welcome! Feel free to submit issues, fork the repository, and send pull requests. Your feedback and suggestions for improvement are highly appreciated.

### License

Licensed under the MIT License. See the LICENSE file for details.
# NuExt.Minimal.Mvvm.SourceGenerator

`NuExt.Minimal.Mvvm.SourceGenerator` is an extension for the lightweight MVVM framework [NuExt.Minimal.Mvvm](https://github.com/IvanGit/NuExt.Minimal.Mvvm). This package includes a source generator that produces boilerplate code for your ViewModels at compile time, simplifying development and reducing routine work. By automating repetitive tasks, it helps you focus on implementing application-specific logic.

### Features

- Automatically generates boilerplate code for ViewModels:
  - Generates properties with notification change support.
  - Creates command properties for appropriate methods.
  - Generates static localization classes from JSON files.
- Simplifies the development process by reducing repetitive coding tasks.
- Seamlessly integrates with the NuExt.Minimal.Mvvm framework.
- Enhances maintainability and readability of your codebase.

### Commonly Used Types

- **`Minimal.Mvvm.NotifyAttribute`**: Generates a property for a backing field or a command for a method.
- **`Minimal.Mvvm.AlsoNotifyAttribute`**: Notifies additional properties when the annotated property changes.
- **`Minimal.Mvvm.LocalizeAttribute`**: Localizes the target class using the provided JSON file.
- **`Minimal.Mvvm.CustomAttributeAttribute`**: Specifies a fully qualified attribute name to be applied to a generated property.
- **`Minimal.Mvvm.UseCommandManagerAttribute`**: Enables automatic `CanExecute` reevaluation for the generated command property by subscribing to the WPF `CommandManager.RequerySuggested` event.

### Installation

Via [NuGet](https://www.nuget.org/):

```sh
dotnet add package NuExt.Minimal.Mvvm.SourceGenerator
```

Or via Visual Studio:

1. Go to `Tools -> NuGet Package Manager -> Manage NuGet Packages for Solution...`.
2. Search for `NuExt.Minimal.Mvvm.SourceGenerator`.
3. Click "Install".

### Dependencies

To use this source generator effectively, you need to have any of these packages installed in your project: [`NuExt.Minimal.Mvvm`](https://www.nuget.org/packages/NuExt.Minimal.Mvvm), [`NuExt.Minimal.Mvvm.Wpf`](https://www.nuget.org/packages/NuExt.Minimal.Mvvm.Wpf), or [`NuExt.Minimal.Mvvm.MahApps.Metro`](https://www.nuget.org/packages/NuExt.Minimal.Mvvm.MahApps.Metro). You can add them via NuGet as well:

For the base MVVM framework:
```sh
dotnet add package NuExt.Minimal.Mvvm
```

For Wpf-specific extensions:
```sh
dotnet add package NuExt.Minimal.Mvvm.Wpf
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
using System.Threading.Tasks;

public partial class PersonModel : BindableBase
{
    [Notify, AlsoNotify(nameof(FullName))]
    private string? _name;

    [Notify, AlsoNotify(nameof(FullName))]
    private string? _surname;

    [Notify, AlsoNotify(nameof(FullName))]
    private string? _middleName;

    public string FullName => $"{Surname} {Name} {MiddleName}";

    /// <summary>
    /// Shows information.
    /// </summary>
    [Notify("ShowInfoCommand", Setter = AccessModifier.Private)]
    [CustomAttribute("System.Text.Json.Serialization.JsonIgnore")]
    private async Task ShowAsync(string fullName)
    {
        await Task.Delay(1000);
    }
}
```

The generator could produce the following:

```csharp
partial class PersonModel
{
    public string? Name
    {
        get => _name;
        set
        {
            if (SetProperty(ref _name, value))
            {
                RaisePropertyChanged("FullName");
            }
        }
    }

    public string? Surname
    {
        get => _surname;
        set
        {
            if (SetProperty(ref _surname, value))
            {
                RaisePropertyChanged("FullName");
            }
        }
    }

    public string? MiddleName
    {
        get => _middleName;
        set
        {
            if (SetProperty(ref _middleName, value))
            {
                RaisePropertyChanged("FullName");
            }
        }
    }

    private IAsyncCommand<string>? _showInfoCommand;
    /// <summary>
    /// Shows information.
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public IAsyncCommand<string>? ShowInfoCommand
    {
        get => _showInfoCommand;
        private set => SetProperty(ref _showInfoCommand, value);
    }
}
```

#### UseCommandManagerAttribute

When applied to a command field or method, enables automatic `CanExecute` reevaluation for the generated command property by subscribing to the WPF `CommandManager.RequerySuggested` event. This attribute is used together with `[Notify]` for commands that should react to global UI state changes.

```csharp
using Minimal.Mvvm;

public partial class MyViewModel : ViewModelBase
{
    [Notify, UseCommandManager]
    private IRelayCommand? _saveCommand;
}
```
The source generator creates the command property and automatically adds/removes the subscription:
```csharp
public IRelayCommand? SaveCommand
{
    get => _saveCommand;
    set
    {
        if (SetProperty(ref _saveCommand, value, out var oldValue))
        {
            RequerySuggestedEventManager.RemoveHandler(oldValue);
            RequerySuggestedEventManager.AddHandler(value);
        }
    }
}
```

Now `SaveCommand.CanExecute` will be reevaluated whenever the WPF command manager detects a UI state change (e.g., focus, keyboard, window activation). No manual event wiring is required.

These examples demonstrate how the source generator automatically creates properties with notification changes for fields and methods marked with the `[Notify]` attribute, thereby reducing boilerplate code.

### Contributing

Issues and PRs are welcome. Keep changes minimal and performance-conscious.

### License

MIT. See LICENSE.
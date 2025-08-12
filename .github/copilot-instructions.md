Project Overview
This project is a modular application structured for UWP and .NET Standard 2.0 compatibility. The architecture strictly separates UI, ViewModels, domain, and infrastructure, enforcing dependency injection and adherence to patterns optimized for scalability, testability, and maintainability.

Folder Structure
/View.UWP: UWP UI-specific code and controls (uses XAML).

/ViewModel.UWP: UWP-specific ViewModel implementations.

/ViewContract: .NET Standard 2.0 interfaces for View/ViewModel interactions (no UI-specific APIs).

/Entities: .NET Standard 2.0 data models, contract interfaces, and utility classes.

/Library: .NET Standard 2.0 UseCases, domain logic, DataManagers, DBHandlers, and network abstractions.

Libraries and Frameworks
UWP platform (View.UWP, ViewModel.UWP).

.NET Standard 2.0 as the target for library/shared code (Entities, Library, ViewContract).

Dependency Injection framework for runtime composition (register all ViewModels and DataManagers).

ICommand pattern for UI actions.

Coding Standards
Every control MUST have a ViewModel. Each ViewModel inherits from an abstract base and implements property change notification and IDisposable.

Abstract ViewModels use [ComponentName]VMBase. Concrete ViewModels use [ComponentName]VM.

For all data/database access, UserId is a MANDATORY parameter to every DataManager, UseCase, or DBHandler method.

All fetch/update logic uses: Request object (inheriting the base with OwnerZUID and UserId), UseCase, DataManager interface/implementation, DBHandler.

Ensure all ViewModels and DataManagers are registered for DI, mapping abstractions to implementations.

Implement cleanup and (un)registration for notifications or events in ViewModels (IDisposable).

Always use x:Bind for XAML data bindings; use regular Binding only where x:Bind is not possible. DO NOT use this.DataContext. Access ViewModel via x:Bind ViewModel.Property or x:Bind ViewModel.Command.

Add //TODO: Verify this implementation in C# and <!--TODO: Verify this implementation--> in XAML if anything is uncertain.

Maintain target framework compliance — all shared logic must be .NET Standard 2.0.

UI Guidelines
All data binding in XAML MUST use x:Bind.

Use ICommand for command binding in XAML controls.

No this.DataContext usage.

UI should be modern, maintainable, and UWP-compliant.

Example Patterns
C# (ViewModel):
public abstract class MyWidgetVMBase : TasksViewModelBase {}
public class MyWidgetVM : MyWidgetVMBase, IDisposable
{
    public ICommand LoadDataCommand { get; set; }

    private async void LoadData()
    {
        //TODO: Verify async implementation
        var req = new MyWidgetRequest(RequestType.Load, ownerZUID, userId);
        await useCase.ExecuteAsync(req);
    }

    public void Dispose()
    {
        // Cleanup logic and notification unregistration
    }
}
Request/DataManager:
public class MyWidgetRequest : TasksRequestBase
{
    public string OwnerZUID { get; set; }
    public string UserId { get; set; }
    public MyWidgetRequest(RequestType req, string ownerZUID, string userId) : base(req, ownerZUID)
    { UserId = userId; }
}

public interface IMyWidgetDataManager
{
    void DoWidgetStuff(MyWidgetRequest req, ICallback<MyWidgetResponse> cb);
}

public class MyWidgetDataManager : IMyWidgetDataManager
{
    public void DoWidgetStuff(MyWidgetRequest req, ICallback<MyWidgetResponse> cb)
    {
        DBHandler.DoWidgetStuff(req.OwnerZUID, req.UserId, ...);
    }
}
XAML (View):
<UserControl x:Class="MyApp.MyWidgetControl">
    <Grid>
        <TextBlock Text="{x:Bind ViewModel.Title, Mode=OneWay}" />
        <Button Content="Load" Command="{x:Bind ViewModel.LoadDataCommand}" />
        <!--TODO: Verify complex binding scenario-->
        <ListView ItemsSource="{x:Bind ViewModel.Items, Mode=OneWay}" />
    </Grid>
</UserControl>
Security Note
UserId is required for ALL database operations. Copilot must enforce that NO CRUD/DataManager/DBHandler method ever omits the UserId parameter.

Copilot: Whenever you generate any C# or XAML, ALWAYS follow these rules exactly, including binding, project boundaries, DI, and code patterns.
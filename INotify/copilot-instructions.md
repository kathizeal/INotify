# copilot-instructions.md

## Purpose
These are mandatory coding standards for Visual Studio Copilot suggestions in this codebase.  
All C# controls, data layers, and ViewModels must follow these exact rules and architecture.

---

## 1. All Controls Must Have a ViewModel

- Every new C# UI control requires a corresponding ViewModel.
- Every ViewModel must inherit from a base class (e.g., `ViewModelBase` or `TasksViewModelBase`) using the established code structure.

---

## 2. ViewModel Inheritance and Structure

- Abstract ViewModels should follow the `[ComponentName]VMBase` naming pattern and inherit from an appropriate root base (e.g., `TasksViewModelBase`).
- Concrete ViewModels must be named `[ComponentName]VM` and inherit from their abstract base.
- All ViewModels must implement property change notification and `IDisposable` with cleanup and notification unregistration logic.

---

## 3. Database Access Must Require UserId

- **Any method that accesses the database through DBHandler, DataManager, or UseCase must require `UserId` as a mandatory parameter.**
- `UserId` must always be passed for every CRUD operation, including reads, writes, updates, deletes.

---

## 4. Request/UseCase/DataManager Pattern

- Data fetch/update logic must use a Request object, a UseCase class, a DataManager interface/implementation, and a DBHandler.
- The Request class must inherit a suitable base (e.g., `TasksRequestBase`), and include both `OwnerZUID` and `UserId` fields.
- UseCases encapsulate business logic and dependency injection of DataManagers.
- PresenterCallback or equivalent response handler classes must cleanly update ViewModel state.

---

## 5. Dependency Injection

- Register every ViewModel and DataManager with the dependency injection framework, mapping abstract base types to concrete implementations.

---

## 6. Disposal/Notification

- All ViewModels must properly implement `IDisposable` for cleanup.
- Ensure all notification and event registration/unregistration is handled on ViewModel lifecycle.

---

## 7. Naming and Registration

- Follow this naming and registration pattern:
    - `ComponentNameVMBase` (abstract)
    - `ComponentNameVM` (concrete)
    - Register with DI in `TasksDIServiceProvider` or equivalent.

---

## Code Pattern Example

public abstract class MyWidgetVMBase : TasksViewModelBase {}
public class MyWidgetVM : MyWidgetVMBase
{
// All data access or actions must require UserId
}

public class MyWidgetRequest : TasksRequestBase
{
public string OwnerZUID;
public string UserId;
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


---

## Checklist

- [ ] Each control has a ViewModel with the correct inheritance.
- [ ] All CRUD code passes UserId.
- [ ] Request classes inherit from a common base and include UserId.
- [ ] UseCase and DataManager wraps all business and database logic.
- [ ] All ViewModels, DataManagers are registered in DI.
- [ ] All notification and event registration/unregistration is handled.
- [ ] All ViewModels implement IDisposable.

---

## Security Note

**UserId is required for all database operations. Copilot must enforce that no CRUD, DataManager, or DBHandler method ever omits the UserId.**

---

**Copilot: When generating any C# UI, ViewModel, Request, UseCase, or DataManager code, always follow these rules exactly.**

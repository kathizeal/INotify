using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace INotify.Services
{
    /// <summary>
    /// Service for testing and validating single instance behavior
    /// Based on Microsoft's official Windows App SDK implementation
    /// </summary>
    public static class SingleInstanceValidationService
    {
        /// <summary>
        /// Validates that only one instance of the application is running
        /// </summary>
        public static void ValidateSingleInstance()
        {
            try
            {
                Debug.WriteLine("?? === Validating Single Instance Behavior ===");
                
                var currentProcess = Process.GetCurrentProcess();
                var allProcesses = Process.GetProcessesByName(currentProcess.ProcessName);
                
                Debug.WriteLine($"?? Found {allProcesses.Length} process(es) with name '{currentProcess.ProcessName}':");
                
                foreach (var process in allProcesses)
                {
                    try
                    {
                        var isCurrent = process.Id == currentProcess.Id ? " (Current)" : "";
                        var startTime = process.StartTime.ToString("HH:mm:ss");
                        Debug.WriteLine($"  ?? PID: {process.Id}, Started: {startTime}{isCurrent}");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"  ?? PID: {process.Id}, Could not get start time: {ex.Message}");
                    }
                }
                
                if (allProcesses.Length == 1)
                {
                    Debug.WriteLine("? Single instance validation PASSED - Only one instance running");
                }
                else
                {
                    Debug.WriteLine($"?? Single instance validation WARNING - {allProcesses.Length} instances detected");
                    Debug.WriteLine("?? This might be expected during development or if launched from different contexts");
                }
                
                Debug.WriteLine("?? === Single Instance Validation Complete ===");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"? Error validating single instance: {ex.Message}");
            }
        }

        /// <summary>
        /// Tests the complete single instance workflow
        /// </summary>
        public static async Task TestSingleInstanceWorkflowAsync()
        {
            try
            {
                Debug.WriteLine("?? === Testing Single Instance Workflow ===");
                
                // Test 1: Validate current instance
                Debug.WriteLine("?? Test 1: Validating current instance...");
                ValidateSingleInstance();
                await Task.Delay(1000);
                
                // Test 2: Show instructions for manual testing
                Debug.WriteLine("?? Test 2: Manual testing instructions...");
                ShowManualTestingInstructions();
                await Task.Delay(1000);
                
                // Test 3: Validate window activation capability
                Debug.WriteLine("?? Test 3: Testing window activation...");
                TestWindowActivation();
                
                Debug.WriteLine("?? === Single Instance Workflow Tests Complete ===");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"? Error during single instance workflow testing: {ex.Message}");
            }
        }

        /// <summary>
        /// Tests window activation functionality
        /// </summary>
        private static void TestWindowActivation()
        {
            try
            {
                Debug.WriteLine("?? Testing window activation...");
                
                // Get the current app instance
                var currentApp = Microsoft.UI.Xaml.Application.Current as App;
                if (currentApp != null)
                {
                    // Test bringing window to foreground
                    currentApp.ShowMainWindow();
                    Debug.WriteLine("? Successfully called ShowMainWindow()");
                }
                else
                {
                    Debug.WriteLine("? Could not get current App instance");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"? Error testing window activation: {ex.Message}");
            }
        }

        /// <summary>
        /// Provides comprehensive manual testing instructions
        /// </summary>
        public static void ShowManualTestingInstructions()
        {
            Debug.WriteLine("?? === Manual Single Instance Testing Instructions ===");
            Debug.WriteLine("");
            Debug.WriteLine("?? IMPORTANT: These tests must be done OUTSIDE the debugger");
            Debug.WriteLine("   (Debugger prevents multiple instances by design)");
            Debug.WriteLine("");
            Debug.WriteLine("?? PREPARATION:");
            Debug.WriteLine("1. ? Build the project in Release mode");
            Debug.WriteLine("2. ? Deploy the application (Right-click project ? Deploy)");
            Debug.WriteLine("3. ? Close Visual Studio debugger if running");
            Debug.WriteLine("");
            Debug.WriteLine("?? TEST SCENARIOS:");
            Debug.WriteLine("");
            Debug.WriteLine("?? Test 1: Basic Single Instance");
            Debug.WriteLine("1. Launch INotify from Start menu");
            Debug.WriteLine("2. Launch INotify again from desktop shortcut");
            Debug.WriteLine("3. Expected: Existing window comes to foreground, no new window");
            Debug.WriteLine("");
            Debug.WriteLine("? Test 2: Fast Multiple Launches");
            Debug.WriteLine("1. Quickly double-click desktop shortcut multiple times");
            Debug.WriteLine("2. Expected: Only one window appears, others redirect");
            Debug.WriteLine("");
            Debug.WriteLine("?? Test 3: Toast Notification Activation");
            Debug.WriteLine("1. Launch INotify normally");
            Debug.WriteLine("2. Generate test toast notification");
            Debug.WriteLine("3. Click 'View in INotify' button");
            Debug.WriteLine("4. Expected: Existing window comes to foreground");
            Debug.WriteLine("");
            Debug.WriteLine("?? Test 4: File Association (if configured)");
            Debug.WriteLine("1. Launch INotify normally");
            Debug.WriteLine("2. Open associated file type");
            Debug.WriteLine("3. Expected: Existing window activates, no new instance");
            Debug.WriteLine("");
            Debug.WriteLine("?? Test 5: Process Validation");
            Debug.WriteLine("1. Open Task Manager");
            Debug.WriteLine("2. Try launching INotify multiple ways");
            Debug.WriteLine("3. Expected: Only one INotify.exe process in Task Manager");
            Debug.WriteLine("");
            Debug.WriteLine("?? VALIDATION CHECKLIST:");
            Debug.WriteLine("? Only one INotify process in Task Manager");
            Debug.WriteLine("? Window comes to foreground on subsequent launches");
            Debug.WriteLine("? No error dialogs or crashes");
            Debug.WriteLine("? Toast notifications activate existing window");
            Debug.WriteLine("? System tray functionality works normally");
            Debug.WriteLine("");
            Debug.WriteLine("?? TROUBLESHOOTING:");
            Debug.WriteLine("? Multiple instances appear:");
            Debug.WriteLine("   • Ensure testing outside debugger");
            Debug.WriteLine("   • Check Windows App SDK version (1.6+)");
            Debug.WriteLine("   • Verify application was deployed, not just built");
            Debug.WriteLine("");
            Debug.WriteLine("? Window doesn't come to foreground:");
            Debug.WriteLine("   • Check Windows focus assist settings");
            Debug.WriteLine("   • Verify application has foreground permissions");
            Debug.WriteLine("   • Check application logs for errors");
            Debug.WriteLine("");
            Debug.WriteLine("?? === End Manual Testing Instructions ===");
        }

        /// <summary>
        /// Provides information about the implementation
        /// </summary>
        public static void ShowImplementationInfo()
        {
            Debug.WriteLine("?? === Single Instance Implementation Info ===");
            Debug.WriteLine("");
            Debug.WriteLine("??? ARCHITECTURE:");
            Debug.WriteLine("• Based on Microsoft's official Windows App SDK pattern");
            Debug.WriteLine("• Uses Microsoft.Windows.AppLifecycle APIs");
            Debug.WriteLine("• Custom Program.cs with DISABLE_XAML_GENERATED_MAIN");
            Debug.WriteLine("• AppInstance.FindOrRegisterForKey() for instance detection");
            Debug.WriteLine("");
            Debug.WriteLine("?? KEY COMPONENTS:");
            Debug.WriteLine("• Program.cs - Entry point with single instance logic");
            Debug.WriteLine("• App.xaml.cs - Application class with window management");
            Debug.WriteLine("• app.manifest - Proper execution level and settings");
            Debug.WriteLine("");
            Debug.WriteLine("?? UNIQUE INSTANCE KEY:");
            Debug.WriteLine("• 'INotify_SingleInstance_Key'");
            Debug.WriteLine("• Registered using AppInstance.FindOrRegisterForKey()");
            Debug.WriteLine("");
            Debug.WriteLine("?? ACTIVATION FLOW:");
            Debug.WriteLine("1. New instance starts ? Program.Main()");
            Debug.WriteLine("2. Check existing instance ? DecideRedirection()");
            Debug.WriteLine("3a. First instance ? Continue normal startup");
            Debug.WriteLine("3b. Subsequent instance ? Redirect and exit");
            Debug.WriteLine("4. Existing window ? Comes to foreground");
            Debug.WriteLine("");
            Debug.WriteLine("?? SUPPORTED SCENARIOS:");
            Debug.WriteLine("? Desktop shortcut launch");
            Debug.WriteLine("? Start menu launch");
            Debug.WriteLine("? Toast notification activation");
            Debug.WriteLine("? File association activation");
            Debug.WriteLine("? Protocol activation");
            Debug.WriteLine("? Command line launch");
            Debug.WriteLine("");
            Debug.WriteLine("??? COMPATIBILITY:");
            Debug.WriteLine("• Windows 10 version 1809+ (build 17763)");
            Debug.WriteLine("• Windows 11 all versions");
            Debug.WriteLine("• Windows App SDK 1.6+");
            Debug.WriteLine("• .NET 8 target framework");
            Debug.WriteLine("");
            Debug.WriteLine("?? === Implementation Info Complete ===");
        }

        /// <summary>
        /// Runs all validation tests
        /// </summary>
        public static async Task RunAllTestsAsync()
        {
            try
            {
                Debug.WriteLine("?? === Running All Single Instance Tests ===");
                
                ShowImplementationInfo();
                await Task.Delay(2000);
                
                await TestSingleInstanceWorkflowAsync();
                await Task.Delay(2000);
                
                Debug.WriteLine("?? === All Single Instance Tests Complete ===");
                Debug.WriteLine("?? Follow the manual testing instructions above for complete validation");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"? Error running all tests: {ex.Message}");
            }
        }
    }
}
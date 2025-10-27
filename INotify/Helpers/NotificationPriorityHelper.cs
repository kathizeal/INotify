using System;
using System.Collections.Generic;
using System.Linq;
using AppList;
using INotifyLibrary.Util.Enums;

namespace INotify.Helpers
{
    /// <summary>
    /// Helper class for managing notification priorities and categorization
    /// </summary>
    public static class NotificationPriorityHelper
    {
        /// <summary>
        /// Categorizes apps into priority levels based on various criteria
        /// </summary>
        /// <param name="apps">List of apps to categorize</param>
        /// <returns>Categorized apps by priority</returns>
        public static (List<DndService.PriorityApp> High, List<DndService.PriorityApp> Medium, List<DndService.PriorityApp> Low) 
            CategorizeAppsByPriority(IEnumerable<DndService.PriorityApp> apps)
        {
            var highPriority = new List<DndService.PriorityApp>();
            var mediumPriority = new List<DndService.PriorityApp>();
            var lowPriority = new List<DndService.PriorityApp>();

            foreach (var app in apps)
            {
                var priority = DeterminePriority(app);
                
                switch (priority)
                {
                    case Priority.High:
                        highPriority.Add(app);
                        break;
                    case Priority.Medium:
                        mediumPriority.Add(app);
                        break;
                    case Priority.Low:
                    default:
                        lowPriority.Add(app);
                        break;
                }
            }

            return (highPriority, mediumPriority, lowPriority);
        }

        /// <summary>
        /// Determines the priority level of an app based on its characteristics
        /// </summary>
        /// <param name="app">App to evaluate</param>
        /// <returns>Suggested priority level</returns>
        public static Priority DeterminePriority(DndService.PriorityApp app)
        {
            if (app == null || string.IsNullOrEmpty(app.DisplayName))
                return Priority.Low;

            var name = app.DisplayName.ToLower();
            var publisher = app.Publisher?.ToLower() ?? "";

            // High Priority: Security, System, Communication
            if (IsHighPriorityApp(name, publisher))
                return Priority.High;

            // Medium Priority: Productivity, Work-related
            if (IsMediumPriorityApp(name, publisher))
                return Priority.Medium;

            // Low Priority: Games, Entertainment, etc.
            return Priority.Low;
        }

        private static bool IsHighPriorityApp(string name, string publisher)
        {
            var highPriorityKeywords = new[]
            {
                "security", "antivirus", "firewall", "defender", "system", "windows security",
                "phone", "teams", "skype", "discord", "telegram", "whatsapp", "signal",
                "emergency", "alarm", "timer", "calendar", "reminder",
                "authenticator", "banking", "finance", "wallet"
            };

            var highPriorityPublishers = new[]
            {
                "microsoft corporation", "google", "apple", "meta", "telegram",
                "signal foundation", "discord inc"
            };

            return highPriorityKeywords.Any(keyword => name.Contains(keyword)) ||
                   highPriorityPublishers.Any(pub => publisher.Contains(pub));
        }

        private static bool IsMediumPriorityApp(string name, string publisher)
        {
            var mediumPriorityKeywords = new[]
            {
                "mail", "outlook", "gmail", "thunderbird", "calendar",
                "office", "word", "excel", "powerpoint", "teams",
                "slack", "zoom", "webex", "gotomeeting",
                "visual studio", "code", "notepad", "editor",
                "browser", "chrome", "firefox", "edge",
                "weather", "news", "stocks", "finance"
            };

            var mediumPriorityPublishers = new[]
            {
                "microsoft corporation", "google llc", "mozilla",
                "slack technologies", "zoom"
            };

            return mediumPriorityKeywords.Any(keyword => name.Contains(keyword)) ||
                   mediumPriorityPublishers.Any(pub => publisher.Contains(pub));
        }

        /// <summary>
        /// Gets user-friendly descriptions for priority levels
        /// </summary>
        /// <param name="priority">Priority level</param>
        /// <returns>Description and emoji</returns>
        public static (string Emoji, string Description) GetPriorityInfo(Priority priority)
        {
            return priority switch
            {
                Priority.High => ("??", "Critical notifications that break through DND"),
                Priority.Medium => ("??", "Important notifications with moderate priority"),
                Priority.Low => ("??", "Standard notifications with normal handling"),
                _ => ("?", "No specific priority assigned")
            };
        }

        /// <summary>
        /// Suggests apps for spaces based on their type and usage
        /// </summary>
        /// <param name="apps">Available apps</param>
        /// <returns>Suggested app groupings for spaces</returns>
        public static Dictionary<string, List<DndService.PriorityApp>> SuggestSpaceGroupings(IEnumerable<DndService.PriorityApp> apps)
        {
            var suggestions = new Dictionary<string, List<DndService.PriorityApp>>
            {
                ["Work & Productivity"] = new List<DndService.PriorityApp>(),
                ["Communication"] = new List<DndService.PriorityApp>(),
                ["System & Security"] = new List<DndService.PriorityApp>()
            };

            foreach (var app in apps)
            {
                var name = app.DisplayName.ToLower();
                
                if (IsWorkProductivityApp(name))
                    suggestions["Work & Productivity"].Add(app);
                else if (IsCommunicationApp(name))
                    suggestions["Communication"].Add(app);
                else if (IsSystemSecurityApp(name))
                    suggestions["System & Security"].Add(app);
            }

            return suggestions;
        }

        private static bool IsWorkProductivityApp(string name)
        {
            var keywords = new[] { "office", "word", "excel", "powerpoint", "outlook", "onenote", 
                                  "visual studio", "code", "notepad", "calculator", "calendar" };
            return keywords.Any(keyword => name.Contains(keyword));
        }

        private static bool IsCommunicationApp(string name)
        {
            var keywords = new[] { "teams", "skype", "discord", "telegram", "whatsapp", "signal",
                                  "slack", "zoom", "phone", "mail", "messenger" };
            return keywords.Any(keyword => name.Contains(keyword));
        }

        private static bool IsSystemSecurityApp(string name)
        {
            var keywords = new[] { "security", "defender", "antivirus", "firewall", "system",
                                  "settings", "control panel", "task manager", "device manager" };
            return keywords.Any(keyword => name.Contains(keyword));
        }
    }
}
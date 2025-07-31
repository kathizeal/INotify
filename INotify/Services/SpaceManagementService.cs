using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using INotifyLibrary.Model.Entity;
using INotify.ViewModels;

namespace INotify.Services
{
    /// <summary>
    /// Service for managing space-package mappings using KSpaceMapper
    /// </summary>
    public class SpaceManagementService
    {
        private readonly List<KSpaceMapper> _spaceMappers = new();
        private readonly List<KSpace> _spaces = new();

        public SpaceManagementService()
        {
            InitializeDefaultSpaces();
        }

        /// <summary>
        /// Initialize default spaces
        /// </summary>
        private void InitializeDefaultSpaces()
        {
            _spaces.AddRange(new[]
            {
                new KSpace 
                { 
                    SpaceId = "space1", 
                    SpaceName = "Work Space", 
                    SpaceDescription = "Work-related applications and notifications"
                },
                new KSpace 
                { 
                    SpaceId = "space2", 
                    SpaceName = "Personal Space", 
                    SpaceDescription = "Personal applications and notifications"
                },
                new KSpace 
                { 
                    SpaceId = "space3", 
                    SpaceName = "Entertainment Space", 
                    SpaceDescription = "Games, media, and entertainment apps"
                }
            });
        }

        /// <summary>
        /// Gets all available spaces
        /// </summary>
        public List<KSpace> GetAllSpaces()
        {
            return _spaces.ToList();
        }

        /// <summary>
        /// Gets packages in a specific space
        /// </summary>
        public List<string> GetPackagesInSpace(string spaceId)
        {
            return _spaceMappers
                .Where(mapper => mapper.SpaceId == spaceId)
                .Select(mapper => mapper.PackageId)
                .ToList();
        }

        /// <summary>
        /// Gets spaces containing a specific package
        /// </summary>
        public List<string> GetSpacesForPackage(string packageId)
        {
            return _spaceMappers
                .Where(mapper => mapper.PackageId == packageId)
                .Select(mapper => mapper.SpaceId)
                .ToList();
        }

        /// <summary>
        /// Adds a package to a space
        /// </summary>
        public bool AddPackageToSpace(string packageId, string spaceId)
        {
            try
            {
                // Check if mapping already exists
                if (_spaceMappers.Any(m => m.PackageId == packageId && m.SpaceId == spaceId))
                {
                    return false; // Already exists
                }

                var mapper = new KSpaceMapper
                {
                    PackageId = packageId,
                    SpaceId = spaceId
                };

                _spaceMappers.Add(mapper);
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error adding package to space: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Removes a package from a space
        /// </summary>
        public bool RemovePackageFromSpace(string packageId, string spaceId)
        {
            try
            {
                var mapper = _spaceMappers.FirstOrDefault(m => m.PackageId == packageId && m.SpaceId == spaceId);
                if (mapper != null)
                {
                    _spaceMappers.Remove(mapper);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error removing package from space: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Gets space statistics
        /// </summary>
        public Dictionary<string, int> GetSpaceStatistics()
        {
            var stats = new Dictionary<string, int>();
            
            foreach (var space in _spaces)
            {
                var packageCount = _spaceMappers.Count(m => m.SpaceId == space.SpaceId);
                stats[space.SpaceId] = packageCount;
            }
            
            return stats;
        }

        /// <summary>
        /// Creates SpaceViewModel objects for UI binding
        /// </summary>
        public List<SpaceViewModel> GetSpaceViewModels()
        {
            var viewModels = new List<SpaceViewModel>();
            var stats = GetSpaceStatistics();

            foreach (var space in _spaces)
            {
                var viewModel = new SpaceViewModel
                {
                    SpaceId = space.SpaceId,
                    DisplayName = space.SpaceName,
                    Description = space.SpaceDescription,
                    PackageCount = stats.GetValueOrDefault(space.SpaceId, 0),
                    NotificationCount = new Random().Next(0, 50), // In real implementation, count actual notifications
                    IsActive = true // Default to active since KSpace doesn't have this property
                };

                viewModels.Add(viewModel);
            }

            return viewModels;
        }

        /// <summary>
        /// Creates a new space
        /// </summary>
        public bool CreateSpace(string spaceId, string spaceName, string description)
        {
            try
            {
                if (_spaces.Any(s => s.SpaceId == spaceId))
                {
                    return false; // Space already exists
                }

                var newSpace = new KSpace
                {
                    SpaceId = spaceId,
                    SpaceName = spaceName,
                    SpaceDescription = description
                };

                _spaces.Add(newSpace);
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creating space: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Updates space information
        /// </summary>
        public bool UpdateSpace(string spaceId, string spaceName, string description)
        {
            try
            {
                var space = _spaces.FirstOrDefault(s => s.SpaceId == spaceId);
                if (space != null)
                {
                    space.SpaceName = spaceName;
                    space.SpaceDescription = description;
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating space: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Deletes a space and all its mappings
        /// </summary>
        public bool DeleteSpace(string spaceId)
        {
            try
            {
                // Remove all mappings for this space
                _spaceMappers.RemoveAll(m => m.SpaceId == spaceId);
                
                // Remove the space itself
                var space = _spaces.FirstOrDefault(s => s.SpaceId == spaceId);
                if (space != null)
                {
                    _spaces.Remove(space);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error deleting space: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Gets suggested spaces for a package based on its characteristics
        /// </summary>
        public List<string> GetSuggestedSpacesForPackage(string packageId, string displayName, string publisher)
        {
            var suggestions = new List<string>();

            // Work space suggestions
            if (IsWorkRelated(displayName, publisher))
                suggestions.Add("space1");

            // Personal space suggestions
            if (IsPersonalApp(displayName, publisher))
                suggestions.Add("space2");

            // Entertainment space suggestions
            if (IsEntertainmentApp(displayName, publisher))
                suggestions.Add("space3");

            return suggestions;
        }

        private bool IsWorkRelated(string displayName, string publisher)
        {
            var workKeywords = new[] { "office", "teams", "outlook", "excel", "word", "powerpoint", "sharepoint", "onedrive", "microsoft", "work", "business", "enterprise" };
            var name = displayName.ToLower();
            var pub = publisher.ToLower();
            return workKeywords.Any(keyword => name.Contains(keyword) || pub.Contains(keyword));
        }

        private bool IsPersonalApp(string displayName, string publisher)
        {
            var personalKeywords = new[] { "mail", "calendar", "photos", "contacts", "notes", "weather", "news", "maps", "personal", "family" };
            var name = displayName.ToLower();
            var pub = publisher.ToLower();
            return personalKeywords.Any(keyword => name.Contains(keyword) || pub.Contains(keyword));
        }

        private bool IsEntertainmentApp(string displayName, string publisher)
        {
            var entertainmentKeywords = new[] { "game", "music", "video", "movie", "tv", "media", "player", "streaming", "entertainment", "fun", "sport" };
            var name = displayName.ToLower();
            var pub = publisher.ToLower();
            return entertainmentKeywords.Any(keyword => name.Contains(keyword) || pub.Contains(keyword));
        }
    }
}
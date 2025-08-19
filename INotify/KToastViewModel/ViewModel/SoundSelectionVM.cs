using INotify.KToastViewModel.ViewModelContract;
using INotify.Services;
using INotifyLibrary.Domain;
using INotifyLibrary.Model.Entity;
using INotifyLibrary.Util;
using INotifyLibrary.Util.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WinCommon.Error;
using WinCommon.Util;
using WinLogger;

namespace INotify.KToastViewModel.ViewModel
{
    /// <summary>
    /// Concrete ViewModel for sound selection functionality
    /// Follows coding standards with [ComponentName]VM pattern
    /// All database operations require UserId as mandatory parameter
    /// </summary>
    public class SoundSelectionVM : SoundSelectionVMBase, IDisposable
    {
        #region Constructor

        public SoundSelectionVM()
        {
            // Initialize and load sound mappings
            LoadSoundMappings();
        }

        #endregion Constructor

        #region Implementation

        /// <summary>
        /// Loads all sound mappings for the current user
        /// UserId is mandatory parameter per coding standards
        /// </summary>
        public override void LoadSoundMappings()
        {
            try
            {
                IsLoadingSounds = true;
                
                // Create request with required UserId
                var request = SoundMappingRequest.GetAllMappings(INotifyConstant.CurrentUser);
                
                var useCase = new SoundMappingUseCase(request, new LoadSoundMappingsPresenterCallback(this));
                useCase.Execute();
            }
            catch (Exception ex)
            {
                Logger.Error(LogManager.GetCallerInfo(), ex.Message);
                ShowStatusMessage("Failed to load sound mappings", true);
                IsLoadingSounds = false;
            }
        }

        /// <summary>
        /// Updates sound for a specific package
        /// UserId is mandatory parameter per coding standards
        /// </summary>
        /// <param name="packageFamilyName">Package family name</param>
        /// <param name="sound">Sound to assign</param>
        public override void UpdatePackageSound(string packageFamilyName, NotificationSounds sound)
        {
            try
            {
                if (string.IsNullOrEmpty(packageFamilyName))
                {
                    Logger.Error(LogManager.GetCallerInfo(), "PackageFamilyName cannot be null or empty");
                    ShowStatusMessage("Invalid package name", true);
                    return;
                }

                // Create request with required UserId
                var request = SoundMappingRequest.SetPackageSound(packageFamilyName, sound, INotifyConstant.CurrentUser);
                
                var useCase = new SoundMappingUseCase(request, new UpdatePackageSoundPresenterCallback(this, packageFamilyName, sound));
                useCase.Execute();
            }
            catch (Exception ex)
            {
                Logger.Error(LogManager.GetCallerInfo(), ex.Message);
                ShowStatusMessage($"Failed to update sound for {packageFamilyName}", true);
            }
        }

        /// <summary>
        /// Gets sound for a specific package
        /// UserId is mandatory parameter per coding standards
        /// </summary>
        /// <param name="packageFamilyName">Package family name</param>
        /// <returns>Associated sound or None if not found</returns>
        public override NotificationSounds GetPackageSound(string packageFamilyName)
        {
            try
            {
                if (string.IsNullOrEmpty(packageFamilyName))
                {
                    return NotificationSounds.None;
                }

                return PackageSoundMap.GetValueOrDefault(packageFamilyName, NotificationSounds.None);
            }
            catch (Exception ex)
            {
                Logger.Error(LogManager.GetCallerInfo(), ex.Message);
                return NotificationSounds.None;
            }
        }

        /// <summary>
        /// Tests a notification sound by playing it
        /// </summary>
        /// <param name="sound">The sound to test</param>
        public override async Task TestSoundAsync(NotificationSounds sound)
        {
            try
            {
                Logger.Info(LogManager.GetCallerInfo(), $"Testing sound: {sound}");
                ShowStatusMessage($"Testing {GetSoundDisplayText(sound)}...");

                var success = await SoundTestService.Instance.TestSoundAsync(sound);
                
                if (success)
                {
                    ShowStatusMessage($"? Playing {GetSoundDisplayText(sound)}");
                }
                else
                {
                    ShowStatusMessage($"? Failed to play {GetSoundDisplayText(sound)}", true);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(LogManager.GetCallerInfo(), ex.Message);
                ShowStatusMessage($"Error testing sound: {ex.Message}", true);
            }
        }

        #endregion Implementation

        #region Presenter Callbacks

        /// <summary>
        /// Presenter callback for loading sound mappings
        /// </summary>
        private class LoadSoundMappingsPresenterCallback : ICallback<SoundMappingResponse>
        {
            private readonly SoundSelectionVM _viewModel;

            public LoadSoundMappingsPresenterCallback(SoundSelectionVM viewModel)
            {
                _viewModel = viewModel;
            }

            public void OnSuccess(ZResponse<SoundMappingResponse> response)
            {
                _viewModel.DispatcherQueue.TryEnqueue(() =>
                {
                    try
                    {
                        _viewModel.SoundMappings.Clear();
                        foreach (var mapping in response.Data.SoundMappings)
                        {
                            _viewModel.SoundMappings.Add(mapping);
                        }

                        _viewModel.UpdateSoundCache(response.Data.SoundMappings);
                        _viewModel.Logger.Info(LogManager.GetCallerInfo(), 
                            $"Loaded {response.Data.SoundMappings.Count} sound mappings");
                    }
                    finally
                    {
                        _viewModel.IsLoadingSounds = false;
                    }
                });
            }

            public void OnProgress(ZResponse<SoundMappingResponse> response)
            {
                // Progress updates if needed
            }

            public void OnFailed(ZResponse<SoundMappingResponse> response)
            {
                _viewModel.DispatcherQueue.TryEnqueue(() =>
                {
                    _viewModel.IsLoadingSounds = false;
                    _viewModel.ShowStatusMessage("Failed to load sound mappings", true);
                    _viewModel.Logger.Error(LogManager.GetCallerInfo(), 
                        $"Failed to load sound mappings: {response.Data?.ErrorMessage}");
                });
            }

            public void OnError(ZError error)
            {
                _viewModel.DispatcherQueue.TryEnqueue(() =>
                {
                    _viewModel.IsLoadingSounds = false;
                    _viewModel.ShowStatusMessage("Error loading sound mappings", true);
                    var errorMessage = error?.ErrorObject?.ToString() ?? "Unknown error";
                    _viewModel.Logger.Error(LogManager.GetCallerInfo(), 
                        $"Error loading sound mappings: {errorMessage}");
                });
            }

            public void OnCanceled(ZResponse<SoundMappingResponse> response)
            {
                _viewModel.DispatcherQueue.TryEnqueue(() =>
                {
                    _viewModel.IsLoadingSounds = false;
                    _viewModel.Logger.Info(LogManager.GetCallerInfo(), "Load sound mappings was canceled");
                });
            }

            public void OnIgnored(ZResponse<SoundMappingResponse> response)
            {
                _viewModel.DispatcherQueue.TryEnqueue(() =>
                {
                    _viewModel.IsLoadingSounds = false;
                    _viewModel.Logger.Info(LogManager.GetCallerInfo(), "Load sound mappings was ignored");
                });
            }
        }

        /// <summary>
        /// Presenter callback for updating package sound
        /// </summary>
        private class UpdatePackageSoundPresenterCallback : ICallback<SoundMappingResponse>
        {
            private readonly SoundSelectionVM _viewModel;
            private readonly string _packageFamilyName;
            private readonly NotificationSounds _sound;

            public UpdatePackageSoundPresenterCallback(SoundSelectionVM viewModel, string packageFamilyName, NotificationSounds sound)
            {
                _viewModel = viewModel;
                _packageFamilyName = packageFamilyName;
                _sound = sound;
            }

            public void OnSuccess(ZResponse<SoundMappingResponse> response)
            {
                _viewModel.DispatcherQueue.TryEnqueue(() =>
                {
                    // Update local cache
                    _viewModel.PackageSoundMap[_packageFamilyName] = _sound;
                    
                    var soundDisplayText = _viewModel.GetSoundDisplayText(_sound);
                    _viewModel.ShowStatusMessage($"Sound updated to {soundDisplayText}");
                    _viewModel.Logger.Info(LogManager.GetCallerInfo(), 
                        $"Updated sound for {_packageFamilyName} to {_sound}");
                });
            }

            public void OnProgress(ZResponse<SoundMappingResponse> response)
            {
                // Progress updates if needed
            }

            public void OnFailed(ZResponse<SoundMappingResponse> response)
            {
                _viewModel.DispatcherQueue.TryEnqueue(() =>
                {
                    _viewModel.ShowStatusMessage("Failed to update sound", true);
                    _viewModel.Logger.Error(LogManager.GetCallerInfo(), 
                        $"Failed to update sound for {_packageFamilyName}: {response.Data?.ErrorMessage}");
                });
            }

            public void OnError(ZError error)
            {
                _viewModel.DispatcherQueue.TryEnqueue(() =>
                {
                    _viewModel.ShowStatusMessage("Error updating sound", true);
                    var errorMessage = error?.ErrorObject?.ToString() ?? "Unknown error";
                    _viewModel.Logger.Error(LogManager.GetCallerInfo(), 
                        $"Error updating sound for {_packageFamilyName}: {errorMessage}");
                });
            }

            public void OnCanceled(ZResponse<SoundMappingResponse> response)
            {
                _viewModel.DispatcherQueue.TryEnqueue(() =>
                {
                    _viewModel.Logger.Info(LogManager.GetCallerInfo(), 
                        $"Update sound for {_packageFamilyName} was canceled");
                });
            }

            public void OnIgnored(ZResponse<SoundMappingResponse> response)
            {
                _viewModel.DispatcherQueue.TryEnqueue(() =>
                {
                    _viewModel.Logger.Info(LogManager.GetCallerInfo(), 
                        $"Update sound for {_packageFamilyName} was ignored");
                });
            }
        }

        #endregion Presenter Callbacks

        #region Cleanup

        public override void Dispose()
        {
            base.Dispose();
        }

        #endregion Cleanup
    }
}
using INotify.KToastView.Model;
using INotify.KToastViewModel.ViewModelContract;
using INotifyLibrary.Domain;
using INotifyLibrary.Model.Entity;
using INotifyLibrary.Util;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinCommon.Error;
using WinCommon.Util;

namespace INotify.KToastViewModel.ViewModel
{
    public class KSpaceViewModel : KSpaceViewModelBase
    {
        public KSpaceViewModel()
        {
                
        }
        public override void AddToSpace(string packageId, string spaceId,string userId)
        {
            var addPackageToSpaceRequest = new AddPackageToSpaceRequest(spaceId, packageId, userId);
            var addPackageToSpace = new AddPackageToSpace(addPackageToSpaceRequest, new AddPackageToSpacePresenterCallback(this));
            addPackageToSpace.Execute();
        }

        public override void GetAllSpace(string userId)
        {
            var getAllSpaceRequest = new GetAllSpaceRequest(userId);
            var getAllSpace = new GetAllSpace(getAllSpaceRequest, new GetAllSpacePresenterCallback(this));
            getAllSpace.Execute();
        }

        public override void RemoveFromSpace(string packageId, string spaceId,string userId)
        {
            var removePackageFromSpaceRequest = new RemovePackageFromSpaceRequest(spaceId, packageId, userId);
            var removePackageFromSpace = new RemovePackageFromSpace(removePackageFromSpaceRequest, new RemovePackageFromSpacePresenterCallback(this));
            removePackageFromSpace.Execute();
        }

        private void PopulateSpaces(ObservableCollection<KSpace> spaces)
        {
            foreach (var space in spaces)
            {
                var spaceVObj = new KSpaceVObj();
                spaceVObj.Update(space);
                //spaceVObj.SetViewSpaceIcon();
                KSpaceList.Add(spaceVObj);
            }
        }

        public override void GetPackagesBySpace(string spaceId,string userId)
        {
            var getPackageBySpaceRequest = new GetPackageBySpaceRequest(spaceId, userId);
            var getPackageBySpace = new GetPackageBySpace(getPackageBySpaceRequest, new GetPackageBySpacePresenterCallback(this));
            getPackageBySpace.Execute();
        }


        private class AddPackageToSpacePresenterCallback : IAddPackageToSpacePresenterCallback
        {
            private readonly KSpaceViewModel _viewModel;

            public AddPackageToSpacePresenterCallback(KSpaceViewModel viewModel)
            {
                _viewModel = viewModel;
            }

            public void OnCanceled(ZResponse<AddPackageToSpaceResponse> response)
            {
                // Handle cancellation
            }

            public void OnError(ZError error)
            {
                // Handle error
            }

            public void OnFailed(ZResponse<AddPackageToSpaceResponse> response)
            {
                // Handle failure
            }

            public void OnIgnored(ZResponse<AddPackageToSpaceResponse> response)
            {
                // Handle ignored response
            }

            public void OnProgress(ZResponse<AddPackageToSpaceResponse> response)
            {
                // Handle progress
            }

            public void OnSuccess(ZResponse<AddPackageToSpaceResponse> response)
            {
                // Handle success
            }
        }

        private class GetAllSpacePresenterCallback : IGetAllSpacePresenterCallback
        {
            private readonly KSpaceViewModel _viewModel;

            public GetAllSpacePresenterCallback(KSpaceViewModel viewModel)
            {
                _viewModel = viewModel;
            }

            public void OnCanceled(ZResponse<GetAllSpaceResponse> response)
            {
                // Handle cancellation
            }

            public void OnError(ZError error)
            {
                // Handle error
            }

            public void OnFailed(ZResponse<GetAllSpaceResponse> response)
            {
                // Handle failure
            }

            public void OnIgnored(ZResponse<GetAllSpaceResponse> response)
            {
                // Handle ignored response
            }

            public void OnProgress(ZResponse<GetAllSpaceResponse> response)
            {
                // Handle progress
            }

            public void OnSuccess(ZResponse<GetAllSpaceResponse> response)
            {
                _viewModel.DispatcherQueue.TryEnqueue(() =>
                {
                    _viewModel.PopulateSpaces(response.Data.Spaces);
                });
            }
        }

     

        private class RemovePackageFromSpacePresenterCallback : IRemovePackageFromSpacePresenterCallback
        {
            private readonly KSpaceViewModel _viewModel;

            public RemovePackageFromSpacePresenterCallback(KSpaceViewModel viewModel)
            {
                _viewModel = viewModel;
            }

            public void OnCanceled(ZResponse<RemovePackageFromSpaceResponse> response)
            {
                // Handle cancellation
            }

            public void OnError(ZError error)
            {
                // Handle error
            }

            public void OnFailed(ZResponse<RemovePackageFromSpaceResponse> response)
            {
                // Handle failure
            }

            public void OnIgnored(ZResponse<RemovePackageFromSpaceResponse> response)
            {
                // Handle ignored response
            }

            public void OnProgress(ZResponse<RemovePackageFromSpaceResponse> response)
            {
                // Handle progress
            }

            public void OnSuccess(ZResponse<RemovePackageFromSpaceResponse> response)
            {
                // Handle success
            }
        }

        private class GetPackageBySpacePresenterCallback : IGetPackageBySpacePresenterCallback
        {
            private readonly KSpaceViewModel _viewModel;

            public GetPackageBySpacePresenterCallback(KSpaceViewModel viewModel)
            {
                _viewModel = viewModel;
            }

            public void OnCanceled(ZResponse<GetPackageBySpaceResponse> response)
            {
                // Handle cancellation
            }

            public void OnError(ZError error)
            {
                // Handle error
            }

            public void OnFailed(ZResponse<GetPackageBySpaceResponse> response)
            {
                // Handle failure
            }

            public void OnIgnored(ZResponse<GetPackageBySpaceResponse> response)
            {
                // Handle ignored response
            }

            public void OnProgress(ZResponse<GetPackageBySpaceResponse> response)
            {
                // Handle progress
            }

            public void OnSuccess(ZResponse<GetPackageBySpaceResponse> response)
            {
                _viewModel.DispatcherQueue.TryEnqueue(() =>
                {
                    _viewModel.ShowPackagesFlyout(response.Data.Packages);
                });
            }
        }

        private void ShowPackagesFlyout(ObservableCollection<KPackageProfile> packages)
        {
            SpaceView?.ShowPackagesFlyout(packages);
        }

     
    }
}
                                    
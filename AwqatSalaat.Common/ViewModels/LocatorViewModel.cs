using AwqatSalaat.Data;
using AwqatSalaat.Helpers;
using AwqatSalaat.Properties;
using AwqatSalaat.Services.Nominatim;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace AwqatSalaat.ViewModels
{
    public class LocatorViewModel : ObservableObject
    {
        private bool isBusy;
        private bool isChecking;
        private bool pendingCheck;
        private string query;
        private string error;
        private IList<Place> places;
        private Place selectedPlace;
        private Place pendingPlace;
        private CancellationTokenSource cancellationTokenSource;

        private Settings Settings => Settings.Default;

        public bool DetectByCountryCode
        {
            get => Settings.LocationDetection == LocationDetectionMode.ByCountryCode;
            set
            {
                if (value)
                {
                    Settings.LocationDetection = LocationDetectionMode.ByCountryCode;
                }
            }
        }
        public bool DetectByCoordinates
        {
            get => Settings.LocationDetection == LocationDetectionMode.ByCoordinates;
            set
            {
                if (value)
                {
                    Settings.LocationDetection = LocationDetectionMode.ByCoordinates;
                }
            }
        }
        public bool DetectByQuery
        {
            get => Settings.LocationDetection == LocationDetectionMode.ByQuery;
            set
            {
                if (value)
                {
                    Settings.LocationDetection = LocationDetectionMode.ByQuery;
                }
            }
        }
        public bool HasPlaces => places?.Count > 0;
        public bool HasQuery => !string.IsNullOrWhiteSpace(query);
        public bool HasError => !string.IsNullOrWhiteSpace(error);
        public bool HasCheckResult => !(pendingPlace is null);
        public bool IsBusy { get => isBusy; private set => SetProperty(ref isBusy, value); }
        public bool IsChecking { get => isChecking; private set => SetProperty(ref isChecking, value); }
        public bool PendingCheck { get => pendingCheck; private set => SetProperty(ref pendingCheck, value); }
        public string SearchQuery { get => query; set { SetProperty(ref query, value); OnPropertyChanged(nameof(HasQuery)); Query(value); } }
        public string Error { get => error; private set { SetProperty(ref error, value); OnPropertyChanged(nameof(HasError)); } }
        public IList<Place> Places { get => places; private set { SetProperty(ref places, value); OnPropertyChanged(nameof(HasPlaces)); } }
        public Place PendingPlace { get => pendingPlace; private set { SetProperty(ref pendingPlace, value); OnPropertyChanged(nameof(HasCheckResult)); } }
        public Place SelectedPlace { get => selectedPlace; set => SetPlace(value); }

        public ICommand Check { get; }
        public ICommand ConfirmCheck { get; }
        public ICommand CancelCheck { get; }

        public LocatorViewModel()
        {
            Check = new RelayCommand(o => CheckExecute());
            ConfirmCheck = new RelayCommand(ConfirmCheckExecute);
            CancelCheck = new RelayCommand(CancelCheckExecute);

            Settings.PropertyChanged += Settings_PropertyChanged;
        }

        private void Settings_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Settings.LocationDetection))
            {
                OnPropertyChanged(nameof(DetectByCountryCode));
                OnPropertyChanged(nameof(DetectByCoordinates));
                OnPropertyChanged(nameof(DetectByQuery));

                if (!DetectByCoordinates && PendingCheck)
                {
                    CancelCheckExecute(null);
                }
            }
        }

        private void ConfirmCheckExecute(object obj)
        {
            SetPlace(pendingPlace);
            PendingPlace = null;
            PendingCheck = false;
        }

        private void CancelCheckExecute(object obj)
        {
            cancellationTokenSource?.Cancel();
            PendingCheck = false;

            Reset();
        }

        private async Task CheckExecute()
        {
            cancellationTokenSource?.Cancel();

            try
            {
                Reset();

                IsChecking = true;
                PendingCheck = true;

                cancellationTokenSource = new CancellationTokenSource();
                var result = await NominatimClient.Reverse(Settings.Latitude, Settings.Longitude, cancellationTokenSource.Token);

                if (IsValidPlace(result))
                {
                    PendingPlace = result;
                }
            }
            catch (NominatimException nx)
            {
                Error = nx.Message;
            }
            finally
            {
                IsChecking = false;
                cancellationTokenSource = null;
            }
        }

        private async Task Query(string query)
        {
            cancellationTokenSource?.Cancel();

            try
            {
                Reset();

                if (!string.IsNullOrEmpty(query))
                {
                    IsBusy = true;

                    cancellationTokenSource = new CancellationTokenSource();
                    var result = await NominatimClient.Search(query, cancellationTokenSource.Token);
                    Places = result?.Where(IsValidPlace).ToArray();
                }
            }
            catch (NominatimException nx)
            {
                Error = nx.Message;
            }
            finally
            {
                IsBusy = false;
                cancellationTokenSource = null;
            }
        }

        private bool IsValidPlace(Place place)
        {
            return !string.IsNullOrEmpty(place.Address?.City)
                && CountriesProvider.GetCountries().Any(c => string.Equals(c.Code, place.Address.CountryCode, StringComparison.OrdinalIgnoreCase));
        }

        private void Reset()
        {
            Error = null;
            Places = null;
            SelectedPlace = null;
            PendingPlace = null;
        }

        private void SetPlace(Place place)
        {
            selectedPlace = place;
            OnPropertyChanged(nameof(SelectedPlace));

            if (place is null)
            {
                return;
            }

            Settings.CountryCode = place.Address.CountryCode.ToUpper();
            Settings.City = place.Address.City;
            Settings.ZipCode = null;
            Settings.Latitude = place.Latitude;
            Settings.Longitude = place.Longitude;
        }
    }
}

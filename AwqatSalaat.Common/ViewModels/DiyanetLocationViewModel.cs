using AwqatSalaat.Helpers;
using AwqatSalaat.Properties;
using AwqatSalaat.Services.Diyanet;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AwqatSalaat.ViewModels
{
    /// <summary>
    /// Picks a Diyanet district: country > city > district. The lists come from the API and are loaded
    /// when the Diyanet service is selected. The choice is written to the (realtime) settings.
    /// </summary>
    public class DiyanetLocationViewModel : ObservableObject
    {
        private readonly Settings settings;
        private IList<DiyanetPlace> countries, cities, districts;
        private DiyanetPlace country, city, district;
        private bool isBusy, restoring;
        private string error;

        // Tests replace the API calls
        internal Func<Task<IList<DiyanetPlace>>> LoadCountries = DiyanetClient.GetCountriesAsync;
        internal Func<int, Task<IList<DiyanetPlace>>> LoadCities = DiyanetClient.GetCitiesAsync;
        internal Func<int, Task<IList<DiyanetPlace>>> LoadDistricts = DiyanetClient.GetDistrictsAsync;

        public IList<DiyanetPlace> Countries { get => countries; private set => SetProperty(ref countries, value); }
        public IList<DiyanetPlace> Cities { get => cities; private set => SetProperty(ref cities, value); }
        public IList<DiyanetPlace> Districts { get => districts; private set => SetProperty(ref districts, value); }
        public bool IsBusy { get => isBusy; private set => SetProperty(ref isBusy, value); }
        public string Error { get => error; private set { SetProperty(ref error, value); OnPropertyChanged(nameof(HasError)); } }
        public bool HasError => !string.IsNullOrEmpty(error);

        public DiyanetPlace SelectedCountry
        {
            get => country;
            set
            {
                if (SetProperty(ref country, value) && !restoring && value != null)
                {
                    settings.DiyanetCountry = value.Id;
                    settings.DiyanetCountryName = value.Name;
                    _ = LoadCitiesAsync(value.Id, selectId: 0);
                }
            }
        }

        public DiyanetPlace SelectedCity
        {
            get => city;
            set
            {
                if (SetProperty(ref city, value) && !restoring && value != null)
                {
                    settings.DiyanetCity = value.Id;
                    _ = LoadDistrictsAsync(value.Id, selectId: 0);
                }
            }
        }

        public DiyanetPlace SelectedDistrict
        {
            get => district;
            set
            {
                if (SetProperty(ref district, value) && !restoring && value != null)
                {
                    settings.DiyanetDistrict = value.Id;
                    settings.DiyanetDistrictName = value.Name;
                }
            }
        }

        public DiyanetLocationViewModel(Settings settings)
        {
            this.settings = settings;
        }

        /// <summary>Loads the lists once and selects the saved country, city and district.</summary>
        public async Task EnsureLoadedAsync()
        {
            if (countries?.Count > 0 || isBusy)
            {
                return;
            }

            await RunAsync(async () =>
            {
                var list = await LoadCountries();
                restoring = true;
                Countries = list;
                SelectedCountry = list.FirstOrDefault(c => c.Id == settings.DiyanetCountry);
                restoring = false;

                if (country != null)
                {
                    await LoadCitiesCoreAsync(country.Id, settings.DiyanetCity);
                }
            });
        }

        private Task LoadCitiesAsync(int countryId, int selectId) => RunAsync(() => LoadCitiesCoreAsync(countryId, selectId));

        private Task LoadDistrictsAsync(int cityId, int selectId) => RunAsync(() => LoadDistrictsCoreAsync(cityId, selectId));

        private async Task LoadCitiesCoreAsync(int countryId, int selectId)
        {
            ClearDistrict();
            Cities = null;
            var list = await LoadCities(countryId);
            Cities = list;
            // Some countries have a single "city" (e.g. Austria), select it to save a click
            var selected = list.FirstOrDefault(c => c.Id == selectId) ?? (list.Count == 1 ? list[0] : null);

            restoring = true;
            SelectedCity = selected;
            restoring = false;

            if (selected != null)
            {
                settings.DiyanetCity = selected.Id;
                await LoadDistrictsCoreAsync(selected.Id, selected.Id == selectId ? settings.DiyanetDistrict : 0);
            }
        }

        private async Task LoadDistrictsCoreAsync(int cityId, int selectId)
        {
            ClearDistrict();
            var list = await LoadDistricts(cityId);
            Districts = list;

            restoring = true;
            SelectedDistrict = list.FirstOrDefault(d => d.Id == selectId);
            restoring = false;
        }

        private void ClearDistrict()
        {
            restoring = true;
            Districts = null;
            SelectedDistrict = null;
            restoring = false;

            if (!isRestoringSaved)
            {
                settings.DiyanetDistrict = 0;
                settings.DiyanetDistrictName = null;
            }
        }

        // True while EnsureLoadedAsync restores the saved choice, so it isn't cleared on the way
        private bool isRestoringSaved;

        private async Task RunAsync(Func<Task> action)
        {
            IsBusy = true;
            Error = null;
            isRestoringSaved = countries is null;

            try
            {
                await action();
            }
            catch (Exception ex)
            {
                Log.Warning(ex, $"[Diyanet] Could not load locations: {ex.Message}");
                Error = ex.Message;
            }
            finally
            {
                restoring = false;
                isRestoringSaved = false;
                IsBusy = false;
            }
        }
    }
}

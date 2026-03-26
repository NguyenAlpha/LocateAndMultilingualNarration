using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Mobile.Models;
using Mobile.Services;

namespace Mobile.ViewModels;

public class MapViewModel : INotifyPropertyChanged
{
    private readonly IStallService _stallService;
    private readonly IAudioGuideService _audioGuideService;

    private bool _isLoaded;

    public event PropertyChangedEventHandler? PropertyChanged;
    public event Action<Stall>? FocusStallRequested;
    public event Action? PinsRefreshRequested;

    public ObservableCollection<Stall> Stalls { get; } = [];

    Stall? selectedStall;
    public Stall? SelectedStall
    {
        get => selectedStall;
        set
        {
            if (selectedStall == value) return;
            selectedStall = value;
            OnPropertyChanged();

            if (selectedStall != null)
            {
                FocusStallRequested?.Invoke(selectedStall);
                PinsRefreshRequested?.Invoke();
            }
        }
    }

    bool _isBusy;
    public bool IsBusy
    {
        get => _isBusy;
        set
        {
            if (_isBusy == value) return;
            _isBusy = value;
            OnPropertyChanged();
        }
    }

    string _errorMessage = string.Empty;
    public string ErrorMessage
    {
        get => _errorMessage;
        set
        {
            if (_errorMessage == value) return;
            _errorMessage = value;
            OnPropertyChanged();
        }
    }

    public ICommand RefreshCommand { get; }
    public ICommand PlayAudioCommand { get; }
    public ICommand PauseAudioCommand { get; }
    public ICommand StopAudioCommand { get; }

    public MapViewModel(IStallService stallService, IAudioGuideService audioGuideService)
    {
        _stallService = stallService;
        _audioGuideService = audioGuideService;

        RefreshCommand = new Command(async () => await LoadStallsAsync(true));
        PlayAudioCommand = new Command(async () => await PlayAudioAsync());
        PauseAudioCommand = new Command(PauseAudio);
        StopAudioCommand = new Command(StopAudio);
    }

    public async Task InitializeAsync(string? boothId = null)
    {
        if (!_isLoaded)
        {
            await LoadStallsAsync(false);
            _isLoaded = true;
        }

        if (!string.IsNullOrWhiteSpace(boothId))
        {
            SelectedStall = Stalls.FirstOrDefault(x => x.Id.Equals(boothId, StringComparison.OrdinalIgnoreCase));
        }
    }

    public void SelectStall(Stall stall)
    {
        SelectedStall = stall;
    }

    async Task LoadStallsAsync(bool forceRefresh)
    {
        if (IsBusy)
        {
            return;
        }

        try
        {
            IsBusy = true;
            ErrorMessage = string.Empty;

            var stalls = await _stallService.GetStallsAsync(forceRefresh);

            Stalls.Clear();
            foreach (var stall in stalls)
            {
                Stalls.Add(stall);
            }

            if (Stalls.Count == 0)
            {
                ErrorMessage = "Không có dữ liệu gian hàng để hiển thị.";
            }

            PinsRefreshRequested?.Invoke();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Tải bản đồ thất bại: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    async Task PlayAudioAsync()
    {
        try
        {
            if (SelectedStall is null || string.IsNullOrWhiteSpace(SelectedStall.AudioUrl))
            {
                await Shell.Current.DisplayAlert("Audio", "Gian hàng chưa có audio URL.", "OK");
                return;
            }

            await _audioGuideService.PlayFromUrlAsync(SelectedStall.AudioUrl);
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Audio", $"Không thể phát audio: {ex.Message}", "OK");
        }
    }

    void PauseAudio()
    {
        _audioGuideService.Pause();
    }

    void StopAudio()
    {
        _audioGuideService.Stop();
    }

    void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PS4Remarry.Core;

public enum BatchJobState
{
    Queued,
    Running,
    Completed,
    Failed,
    Cancelled
}

public enum DigestStatus
{
    Unknown,
    Match,
    Mismatch,
    Unreadable
}

public sealed class BatchJob : INotifyPropertyChanged
{
    public int Id { get; }
    public string GamePkg { get; }
    public string UpdatePkg { get; }
    public string OutputDir { get; }

    private BatchJobState _state = BatchJobState.Queued;
    public BatchJobState State
    {
        get => _state;
        set
        {
            if (_state != value)
            {
                _state = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(StateText));
            }
        }
    }

    private int _progress;
    public int Progress
    {
        get => _progress;
        set { if (_progress != value) { _progress = value; OnPropertyChanged(); } }
    }

    private string _status = "Queued";
    public string Status
    {
        get => _status;
        set { if (_status != value) { _status = value; OnPropertyChanged(); } }
    }

    private DigestStatus _digestStatus = DigestStatus.Unknown;
    public DigestStatus DigestStatus
    {
        get => _digestStatus;
        set
        {
            if (_digestStatus != value)
            {
                _digestStatus = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(DigestText));
                OnPropertyChanged(nameof(DigestColorArgb));
            }
        }
    }

    private string _builtPkgPath = "";
    public string BuiltPkgPath
    {
        get => _builtPkgPath;
        set { if (_builtPkgPath != value) { _builtPkgPath = value; OnPropertyChanged(); } }
    }

    public string StateText => State.ToString();

    public string DigestText => DigestStatus switch
    {
        DigestStatus.Match      => "✓ Match",
        DigestStatus.Mismatch   => "✗ Mismatch",
        DigestStatus.Unreadable => "? Unreadable",
        _                       => "",
    };

    public int DigestColorArgb
    {
        get
        {
            switch (DigestStatus)
            {
                case DigestStatus.Match:      return unchecked((int)0xFF008000);
                case DigestStatus.Mismatch:   return unchecked((int)0xFFC00000);
                case DigestStatus.Unreadable: return unchecked((int)0xFF806000);
                default:                      return unchecked((int)0xFF000000);
            }
        }
    }

    public BatchJob(int id, string gamePkg, string updatePkg, string outputDir)
    {
        Id        = id;
        GamePkg   = gamePkg;
        UpdatePkg = updatePkg;
        OutputDir = outputDir;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
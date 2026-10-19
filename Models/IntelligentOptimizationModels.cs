using RyTuneX.Helpers;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace RyTuneX.Models;

public enum OptimizationCategory
{
    Performance,
    PrivacyAndTelemetry,
    SecurityAndPolicies,
    SystemResourcesAndBloat,
    FeaturesAndUsability
}

public enum RiskLevel
{
    Safe,
    Cosmetic,
    Moderate,
    Advanced,
    Caution
}

public enum VerificationStatus
{
    NotVerified,
    VerifiedActive,
    RequiresRestart,
    VerificationFailed
}

public enum OptimizationStep
{
    Scan,
    Analyse,
    Recommend,
    Preview,
    Apply,
    Verify,
    Rollback
}

public class OptimizationItemModel : INotifyPropertyChanged
{
    private string _title = string.Empty;
    private string _description = string.Empty;
    private string _impactDescription = string.Empty;
    private bool _isApplied;
    private bool _isSelectedForApply;
    private VerificationStatus _verificationStatus = VerificationStatus.NotVerified;
    private bool _rollbackAvailable;
    private DateTime? _backupDate;
    private string? _preApplyValue;

    public required string Tag { get; set; }

    public string Title
    {
        get => $"Feature_{Tag}.Header".TryGetLocalized()
            ?? $"Feature_{Tag}/Header".TryGetLocalized()
            ?? $"{Tag}Title".TryGetLocalized()
            ?? (string.IsNullOrEmpty(_title) ? Tag : _title);
        set => _title = value;
    }

    public required OptimizationCategory Category { get; set; }

    public string Description
    {
        get => $"Feature_{Tag}.Description".TryGetLocalized()
            ?? $"Feature_{Tag}/Description".TryGetLocalized()
            ?? _description;
        set => _description = value;
    }

    public string ImpactDescription
    {
        get => $"Intelligent_Impact_{Tag}".TryGetLocalized()
            ?? $"Feature_{Tag}.Impact".TryGetLocalized()
            ?? _impactDescription;
        set => _impactDescription = value;
    }

    public int ScoreWeight { get; set; } = 5;
    public RiskLevel Risk { get; set; } = RiskLevel.Safe;
    public required string TechnicalDetails { get; set; }

    public bool IsApplied
    {
        get => _isApplied;
        set
        {
            if (_isApplied != value)
            {
                _isApplied = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(StatusDisplay));
            }
        }
    }

    public bool IsRecommended { get; set; }
    public string RecommendationReason { get; set; } = string.Empty;

    public bool IsSelectedForApply
    {
        get => _isSelectedForApply;
        set
        {
            if (_isSelectedForApply != value)
            {
                _isSelectedForApply = value;
                OnPropertyChanged();
            }
        }
    }

    public VerificationStatus VerificationStatus
    {
        get => _verificationStatus;
        set
        {
            if (_verificationStatus != value)
            {
                _verificationStatus = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(VerificationStatusDisplay));
            }
        }
    }

    public bool RollbackAvailable
    {
        get => _rollbackAvailable;
        set
        {
            if (_rollbackAvailable != value)
            {
                _rollbackAvailable = value;
                OnPropertyChanged();
            }
        }
    }

    public DateTime? BackupDate
    {
        get => _backupDate;
        set
        {
            if (_backupDate != value)
            {
                _backupDate = value;
                OnPropertyChanged();
            }
        }
    }

    public string? PreApplyValue
    {
        get => _preApplyValue;
        set
        {
            if (_preApplyValue != value)
            {
                _preApplyValue = value;
                OnPropertyChanged();
            }
        }
    }

    public string CategoryDisplay => Category switch
    {
        OptimizationCategory.Performance => "Intelligent_Category_Performance".GetLocalized(),
        OptimizationCategory.PrivacyAndTelemetry => "Intelligent_Category_Privacy".GetLocalized(),
        OptimizationCategory.SecurityAndPolicies => "Shell_Security/Content".TryGetLocalized() ?? "Security",
        OptimizationCategory.SystemResourcesAndBloat => "Shell_Debloat/Content".TryGetLocalized() ?? "Debloat",
        OptimizationCategory.FeaturesAndUsability => "Intelligent_Category_Usability".GetLocalized(),
        _ => "Intelligent_General_Category".GetLocalized()
    };

    public string CategoryGlyph => Category switch
    {
        OptimizationCategory.Performance => "\uE9D9",
        OptimizationCategory.PrivacyAndTelemetry => "\uE7B3",
        OptimizationCategory.SecurityAndPolicies => "\uEA18",
        OptimizationCategory.SystemResourcesAndBloat => "\uE74D",
        OptimizationCategory.FeaturesAndUsability => "\uE74C",
        _ => "\uF259"
    };

    public string RiskDisplay => Risk switch
    {
        RiskLevel.Cosmetic => "Intelligent_Badge_Cosmetic".TryGetLocalized() ?? "Cosmetic",
        RiskLevel.Safe => "Intelligent_Badge_Safe".GetLocalized(),
        RiskLevel.Moderate => "Intelligent_Badge_Moderate".GetLocalized(),
        RiskLevel.Advanced => "Intelligent_Badge_Advanced".GetLocalized(),
        RiskLevel.Caution => "Intelligent_Badge_Caution".GetLocalized(),
        _ => "Intelligent_Badge_Safe".GetLocalized()
    };

    public string StatusDisplay => IsApplied
        ? "Intelligent_Badge_Optimal".GetLocalized()
        : "Intelligent_HealthGrade_NeedsTuning".GetLocalized();

    public string VerificationStatusDisplay => VerificationStatus switch
    {
        VerificationStatus.VerifiedActive => "Intelligent_Verification_Active".GetLocalized(),
        VerificationStatus.RequiresRestart => "Intelligent_Verification_Restart".GetLocalized(),
        VerificationStatus.VerificationFailed => "Intelligent_Verification_Failed".GetLocalized(),
        _ => "Intelligent_Verification_NotVerified".GetLocalized()
    };

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public class IntelligentScoreModel
{
    public int OverallScore { get; set; }
    public int PerformanceScore { get; set; }
    public int PrivacyScore { get; set; }
    public int SecurityScore { get; set; }
    public int ResourcesScore { get; set; }
    public int FeaturesScore { get; set; }
    public int PotentialScoreGain { get; set; }

    public int TotalItemsCount { get; set; }
    public int OptimalItemsCount { get; set; }
    public int RecommendedItemsCount { get; set; }
    public int RollbackableItemsCount { get; set; }

    public string SystemSummary { get; set; } = "Windows PC";
    public string HealthGrade => OverallScore switch
    {
        >= 90 => "Intelligent_HealthGrade_Excellent".GetLocalized(),
        >= 75 => "Intelligent_HealthGrade_Good".GetLocalized(),
        >= 60 => "Intelligent_HealthGrade_Fair".GetLocalized(),
        >= 40 => "Intelligent_HealthGrade_NeedsTuning".GetLocalized(),
        _ => "Intelligent_HealthGrade_Unoptimized".GetLocalized()
    };
}

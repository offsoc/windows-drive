using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using ProtonDrive.App.Authentication;
using ProtonDrive.App.Windows.Configuration.Hyperlinks;
using ProtonDrive.App.Windows.Toolkit;
using ProtonDrive.App.Windows.Toolkit.Threading;
using ProtonDrive.Client;

namespace ProtonDrive.App.Windows.Views.SignIn;

internal sealed class SecondFactorInputViewModel : SessionWorkflowStepViewModelBase, IDeferredValidationResolver
{
    private static readonly Dictionary<MultiFactorAuthenticationMethods, SecondFactorInputPage> AuthenticationMethodToInputPageMapping = new()
        {
            { MultiFactorAuthenticationMethods.Totp, SecondFactorInputPage.Totp },
            { MultiFactorAuthenticationMethods.Fido2, SecondFactorInputPage.Fido2 },
        };

    private readonly AsyncRelayCommand _continueSigningInCommand;
    private readonly IExternalHyperlinks _externalHyperlinks;
    private readonly DispatcherScheduler _scheduler;

    private SecondFactorInputPage _currentPage;
    private bool _isFido2Available;
    private IReadOnlyCollection<SecondFactorInputPage> _enabledPages = [];
    private bool _hasMultipleEnabledPages;
    private string? _code;

    public SecondFactorInputViewModel(
        IAuthenticationService authenticationService,
        IExternalHyperlinks externalHyperlinks,
        DispatcherScheduler scheduler)
        : base(authenticationService)
    {
        _externalHyperlinks = externalHyperlinks;
        _scheduler = scheduler;

        _continueSigningInCommand = new AsyncRelayCommand(ContinueSigningInAsync, CanContinueSigningIn);
        LearnMoreCommand = new RelayCommand(LearnMore);
        SecretFieldMustBeFocused = true;
    }

    public ICommand ContinueSigningInCommand => _continueSigningInCommand;
    public ICommand LearnMoreCommand { get; }

    public SecondFactorInputPage CurrentPage
    {
        get => _currentPage;
        set
        {
            if (SetProperty(ref _currentPage, value))
            {
                Code = null;

                _scheduler.Schedule(() => _continueSigningInCommand.NotifyCanExecuteChanged());
            }
        }
    }

    public IReadOnlyCollection<SecondFactorInputPage> EnabledPages
    {
        get => _enabledPages;
        private set
        {
            if (SetProperty(ref _enabledPages, value))
            {
                HasMultipleEnabledPages = value.Count > 1;
            }
        }
    }

    public bool HasMultipleEnabledPages
    {
        get => _hasMultipleEnabledPages;
        private set => SetProperty(ref _hasMultipleEnabledPages, value);
    }

    public bool IsFido2Available
    {
        get => _isFido2Available;
        private set => SetProperty(ref _isFido2Available, value);
    }

    [DeferredValidation]
    public string? Code
    {
        get => _code;
        set
        {
            if (SetProperty(ref _code, value, true))
            {
                _scheduler.Schedule(() => _continueSigningInCommand.NotifyCanExecuteChanged());
            }
        }
    }

    ValidationResult? IDeferredValidationResolver.Validate(string? memberName)
    {
        switch (memberName)
        {
            case nameof(Code):
                var result = LastResponse is not null && LastResponse.Code != ResponseCode.Success
                    ? new ValidationResult(LastResponse.Error ?? "Incorrect code")
                    : ValidationResult.Success;
                LastResponse = null;
                return result;

            default:
                return ValidationResult.Success;
        }
    }

    public void HandleSessionStateChange(SessionState sessionState)
    {
        if (sessionState.SigningInStatus is not SigningInStatus.WaitingForSecondFactorAuthentication)
        {
            return;
        }

        // Clear 2FA code only on success; otherwise keep it to show validation errors.
        if (sessionState.Response.Succeeded)
        {
            Code = null;
        }

        EnabledPages =
            AuthenticationMethodToInputPageMapping
                .Where(mapping => sessionState.MultiFactorAuthenticationMethods.HasFlag(mapping.Key))
                .Select(mapping => mapping.Value)
                .Distinct()
                .ToList();

        // We keep the current page if it is enabled.
        // We do not want FIDO2 to be current when not available, unless it is the only choice.
        // If no pages are enabled, we still show the default page (NotAvailable).
        CurrentPage =
            EnabledPages.Where(page => page == CurrentPage)
                .Concat(EnabledPages.Where(page => page is not SecondFactorInputPage.Fido2 || sessionState.IsFido2Available))
                .Append(EnabledPages.FirstOrDefault(page => page is SecondFactorInputPage.Fido2))
                .FirstOrDefault();

        IsFido2Available = sessionState.IsFido2Available;
    }

    private bool CanContinueSigningIn()
    {
        return CurrentPage switch
        {
            SecondFactorInputPage.Totp => !string.IsNullOrEmpty(Code),
            SecondFactorInputPage.Fido2 => true,
            SecondFactorInputPage.NotSupported => false,
            _ => throw new ArgumentOutOfRangeException(),
        };
    }

    private async Task ContinueSigningInAsync()
    {
        switch (CurrentPage)
        {
            case SecondFactorInputPage.Totp:
                if (string.IsNullOrEmpty(Code))
                {
                    return;
                }

                await AuthenticationService.AuthenticateWithTotpAsync(Code).ConfigureAwait(true);
                break;

            case SecondFactorInputPage.Fido2:
                await AuthenticationService.AuthenticateWithFido2Async().ConfigureAwait(true);
                break;

            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void LearnMore()
    {
        _externalHyperlinks.HowToUseSecurityKey.Open();
    }
}

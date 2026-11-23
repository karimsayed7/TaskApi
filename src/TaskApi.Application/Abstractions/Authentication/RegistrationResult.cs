namespace TaskApi.Application.Abstractions.Authentication;

public sealed record RegistrationError(
    string Code,
    string Description
);

public sealed record RegistrationResult(
    bool Succeeded,
    IReadOnlyList<RegistrationError> Errors
);
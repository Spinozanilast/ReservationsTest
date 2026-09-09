namespace ReservationBackend.Validation;

public static class Validators
{
    public const string ServiceNameRequiredCode = "service_name_required";
    public const string ServiceDurationInvalidCode = "service_duration_invalid";
    public const string SlotServiceRequiredCode = "service_id_required";
    public const string SlotStartInPastCode = "slot_start_in_past";
    public const string SlotIdRequiredCode = "slot_id_required";
    public const string ClientNameRequiredCode = "client_name_required";
    public const string PhoneNumberInvalidCode = "phone_number_invalid";

    public static (string Code, string Message)? ValidateServiceName(string? name) =>
        string.IsNullOrWhiteSpace(name)
            ? (ServiceNameRequiredCode, "Service name must not be empty.")
            : null;

    public static (string Code, string Message)? ValidateServiceDuration(int duration) =>
        duration <= 0
            ? (ServiceDurationInvalidCode, "Service duration must be greater than zero.")
            : null;

    public static (string Code, string Message)? ValidateSlotServiceId(Guid serviceId) =>
        serviceId == Guid.Empty
            ? (SlotServiceRequiredCode, "ServiceId must be specified.")
            : null;

    public static (string Code, string Message)? ValidateSlotStartTime(DateTime startTime) =>
        startTime <= DateTime.UtcNow
            ? (SlotStartInPastCode, "The slot start time must be in the future.")
            : null;

    public static (string Code, string Message)? ValidateSlotId(Guid slotId) =>
        slotId == Guid.Empty
            ? (SlotIdRequiredCode, "SlotId must be specified.")
            : null;

    public static (string Code, string Message)? ValidateClientName(string? name) =>
        string.IsNullOrWhiteSpace(name)
            ? (ClientNameRequiredCode, "Name must not be empty.")
            : null;

    /// <summary>
    /// Нормализует телефон: допустимы цифры, '+', '-', '(', ')', пробел.
    /// Возвращает нормализованный номер (7–15 цифр, опциональный ведущий '+')
    /// или null, если номер невалиден.
    /// </summary> 
    public static string? NormalizePhoneNumber(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
            return null;

        var trimmed = phone.Trim();
        foreach (var ch in trimmed)
        {
            if (char.IsAsciiDigit(ch) || ch is '+' or '-' or '(' or ')' or ' ')
                continue;
            return null;
        }

        var plusCount = trimmed.Count(ch => ch == '+');
        if (plusCount > 1 || (plusCount == 1 && !trimmed.StartsWith('+')))
            return null;

        var digits = new string(trimmed.Where(char.IsDigit).ToArray());
        if (digits.Length is < 7 or > 15)
            return null;

        return plusCount == 1 ? $"+{digits}" : digits;
    }
}

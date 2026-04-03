namespace IronvaleFleetHub.Api.Common;

/// <summary>
/// Marker interface for commands that intentionally have no FluentValidation validator.
/// The reflection-based coverage test uses this to distinguish deliberate opt-outs
/// from missing validators.
/// </summary>
public interface ISkipValidation { }

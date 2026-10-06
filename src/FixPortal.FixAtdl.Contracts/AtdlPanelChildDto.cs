using System.Text.Json.Serialization;

namespace FixPortal.FixAtdl.Contracts;

/// <summary>
/// Discriminated-union base for the two kinds of child that may appear inside an <see cref="AtdlPanelDto"/>: a nested panel or a control.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
[JsonDerivedType(typeof(AtdlPanelDto), "panel")]
[JsonDerivedType(typeof(AtdlControlDto), "control")]
public abstract record AtdlPanelChildDto;

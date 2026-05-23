// FP Enhancement: 2026-05-23 — replaced System.Configuration-based loader with POCO; .NET 10 has no <configuration> section support.

namespace Atdl4net.Configuration;

/// <summary>
/// Runtime options for the FixAtdl parser and WPF rendering layer.
/// Construct and pass an instance where the API requires configuration;
/// use <see cref="Default"/> for out-of-the-box behaviour.
/// </summary>
public sealed class FixAtdlOptions
{
    public static FixAtdlOptions Default { get; } = new();

    /// <summary>WPF-specific rendering and view-model options.</summary>
    public WpfOptions Wpf { get; init; } = new();

    /// <summary>WPF-specific rendering and view-model options.</summary>
    public sealed class WpfOptions
    {
        /// <summary>
        /// When true, the strategy state is reset whenever a strategy is assigned to a control.
        /// Mirrors upstream <c>wpf/@resetStrategyOnAssignmentToControl</c> (default: true).
        /// </summary>
        public bool ResetStrategyOnAssignmentToControl { get; init; } = true;

        /// <summary>View-specific rendering options.</summary>
        public ViewOptions View { get; init; } = new();

        /// <summary>View-model behaviour options.</summary>
        public ViewModelOptions ViewModel { get; init; } = new();

        /// <summary>View-specific rendering options.</summary>
        public sealed class ViewOptions
        {
            /// <summary>
            /// When true, drop-down controls are automatically sized to fit their content.
            /// Mirrors upstream <c>wpf/view/@autoSizeDropDowns</c> (default: true).
            /// </summary>
            public bool AutoSizeDropDowns { get; init; } = true;
        }

        /// <summary>View-model behaviour options.</summary>
        public sealed class ViewModelOptions
        {
            /// <summary>
            /// When true, validation runs on every value change rather than only on submit.
            /// Mirrors upstream <c>wpf/viewModel/@validateOnChange</c> (default: false).
            /// </summary>
            public bool ValidateOnChange { get; init; } = false;
        }
    }
}

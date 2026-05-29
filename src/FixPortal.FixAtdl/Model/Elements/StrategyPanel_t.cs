// FP Enhancement: 2026-05-24 — modernised for net10 (file-scoped, nullable, FixPortal namespace).
#region Copyright (c) 2010-2011, Steve Wilkinson (author)
//
//   This software is released under the MIT License..
//
#endregion

using System.Collections.Specialized;
using FixPortal.FixAtdl.Model.Collections;
using FixPortal.FixAtdl.Model.Elements.Support;
using FixPortal.FixAtdl.Model.Enumerations;
using FixPortal.FixAtdl.Utility;

namespace FixPortal.FixAtdl.Model.Elements;

/// <summary>
/// Represents a FIXatdl strategy panel and its child controls and panels.
/// </summary>
public class StrategyPanel_t : IParentable<StrategyPanel_t>, IDisposable, IStrategyPanel
{
    private StrategyPanel_t? _owningStrategyPanel;
    private bool _disposed;

    /// <summary>
    /// Gets or sets the border applied to the panel.
    /// </summary>
    public Border_t? Border { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the panel is initially collapsed.
    /// </summary>
    public bool? Collapsed { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the panel can be collapsed.
    /// </summary>
    public bool? Collapsible { get; set; }

    /// <summary>
    /// Gets or sets the panel color.
    /// </summary>
    public string Color { get; set; } = null!;

    /// <summary>
    /// Gets or sets the panel orientation.
    /// </summary>
    public Orientation_t? Orientation { get; set; }

    /// <summary>
    /// Gets or sets the panel title.
    /// </summary>
    public string Title { get; set; } = null!;

    // Single parameter constructor needed for root StrategyPanel_t.
    /// <summary>
    /// Initializes a new root <see cref="StrategyPanel_t"/>.
    /// </summary>
    /// <param name="owner">The owning strategy.</param>
    public StrategyPanel_t(Strategy_t owner) : this(owner, null) { }

    /// <summary>
    /// Initializes a new <see cref="StrategyPanel_t"/>.
    /// </summary>
    /// <param name="owningStrategy">The strategy that owns the panel.</param>
    /// <param name="parent">The parent panel, or <see langword="null"/> when this is a top-level panel.</param>
    public StrategyPanel_t(Strategy_t owningStrategy, IStrategyPanel? parent)
    {
        OwningStrategy = owningStrategy;
        _owningStrategyPanel = parent as StrategyPanel_t;

        // Set defaults
        Collapsed = true;
        Collapsible = false;

        StrategyPanels = [];
    }

    /// <summary>
    /// Gets the strategy that owns this panel.
    /// </summary>
    public Strategy_t OwningStrategy { get; }

    /// <summary>
    /// Gets the child panels contained within this panel.
    /// </summary>
    public StrategyPanelCollection StrategyPanels { get; }

    /// <summary>
    /// Gets the controls contained within this panel.
    /// </summary>
    public ControlCollection Controls
    {
        get
        {
            // Lazy initialisation as we can't use 'this' pointer in constructor.
            if (field == null)
            {
                field = new ControlCollection(this);

                // Provide a mechanism for the Controls collection of the Strategy_t (as opposed to the StrategyPanel_t) to be
                // notified as controls are added to and removed from this StrategyPanel_t.
                field.CollectionChanged += new NotifyCollectionChangedEventHandler(OwningStrategy.Controls.SourceCollectionChanged);
            }

            return field;
        }
    } = null!;

    #region IDisposable Members

    /// <summary>
    /// Releases the managed resources used by the panel.
    /// </summary>
    /// <param name="disposing"><see langword="true"/> to release managed resources; otherwise, <see langword="false"/>.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            if (OwningStrategy != null)
            {
                Controls.CollectionChanged -= new NotifyCollectionChangedEventHandler(OwningStrategy.Controls.SourceCollectionChanged);
            }

            // Recurse over child panels so their Controls -> OwningStrategy subscriptions are released
            // too; disposing only the root panel otherwise leaks every descendant panel's subscription.
            foreach (StrategyPanel_t childPanel in StrategyPanels)
            {
                childPanel.Dispose();
            }
        }

        _disposed = true;
    }

    /// <summary>
    /// Releases resources used by the panel.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);

        GC.SuppressFinalize(this);
    }

    #endregion

    #region IParentable<StrategyPanel_t> Members

    StrategyPanel_t IParentable<StrategyPanel_t>.Parent
    {
        get => _owningStrategyPanel!; set => _owningStrategyPanel = value;
    }

    #endregion
}

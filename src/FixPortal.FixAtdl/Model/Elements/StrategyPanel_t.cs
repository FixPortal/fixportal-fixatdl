// FP Enhancement: 2026-05-24 — modernised for net10 (file-scoped, nullable, FixPortal namespace).
#region Copyright (c) 2010-2011, Steve Wilkinson (author)
//
//   This software is released under the MIT License..
//
#endregion

using System;
using System.Collections.Specialized;
using FixPortal.FixAtdl.Model.Collections;
using FixPortal.FixAtdl.Model.Elements.Support;
using FixPortal.FixAtdl.Model.Enumerations;
using FixPortal.FixAtdl.Utility;

namespace FixPortal.FixAtdl.Model.Elements;

public class StrategyPanel_t : IParentable<StrategyPanel_t>, IDisposable, IStrategyPanel
{
    private StrategyPanel_t? _owningStrategyPanel;

    public Border_t? Border { get; set; }
    public bool? Collapsed { get; set; }
    public bool? Collapsible { get; set; }
    public string Color { get; set; } = null!;
    public Orientation_t? Orientation { get; set; }
    public string Title { get; set; } = null!;

    // Single parameter constructor needed for root StrategyPanel_t.
    public StrategyPanel_t(Strategy_t owner) : this(owner, null) { }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="owningStrategy"></param>
    /// <param name="parent">; null if this StrategyPanel_t does not have a parent (for example, because it is the
    /// immediate descendent of a StrategyLayout_t.</param>
    /// <remarks></remarks>
    public StrategyPanel_t(Strategy_t owningStrategy, IStrategyPanel? parent)
    {
        OwningStrategy = owningStrategy;
        _owningStrategyPanel = parent as StrategyPanel_t;

        // Set defaults
        Collapsed = true;
        Collapsible = false;

        StrategyPanels = [];
    }

    public Strategy_t OwningStrategy { get; }

    public StrategyPanelCollection StrategyPanels { get; }

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

    protected virtual void Dispose(bool disposing)
    {
        if (disposing && OwningStrategy != null)
        {
            Controls.CollectionChanged -= new NotifyCollectionChangedEventHandler(OwningStrategy.Controls.SourceCollectionChanged);
        }
    }

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


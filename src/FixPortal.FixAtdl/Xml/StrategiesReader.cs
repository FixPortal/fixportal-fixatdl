// FP Enhancement: 2026-05-24 — modernised for net10 (file-scoped, nullable, FixPortal namespace).
#region Copyright (c) 2010-2011, Steve Wilkinson (author)
//
//   This software is released under the MIT License..
//
#endregion

using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using FixPortal.FixAtdl.Diagnostics;
using FixPortal.FixAtdl.Diagnostics.Exceptions;
using FixPortal.FixAtdl.Model.Controls;
using FixPortal.FixAtdl.Model.Elements;
using FixPortal.FixAtdl.Resources;
using FixPortal.FixAtdl.Xml.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NodaTime;

namespace FixPortal.FixAtdl.Xml;

/// <summary>
/// Reads and deserializes ATDL strategy definitions from an XML document or stream.
/// Raises <see cref="StrategyLoaded"/> for each strategy as it is deserialized.
/// </summary>
public class StrategiesReader
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<StrategiesReader> _log;
    private readonly IClock _clock;
    private readonly XmlSchemaSet? _schemaSet;
    private readonly IReadOnlyDictionary<string, CustomParameterType> _customParameterTypes;

    /// <summary>
    /// Initializes a new <see cref="StrategiesReader"/>.
    /// </summary>
    /// <param name="loggerFactory">Optional logger factory. When null, no logging is produced
    /// (<see cref="NullLoggerFactory"/>). Supply one to trace loading and deserialization.</param>
    /// <param name="clock">Optional clock wired into every <see cref="Clock_t"/> control as it is
    /// loaded, so a time-only initValue anchors to a controllable "now". When null, the system clock
    /// is used (unchanged behaviour). Supply a NodaTime FakeClock in tests for deterministic dates.</param>
    /// <param name="schemaSet">Optional XML Schema set to validate documents against before structural
    /// deserialization. Not supplied by this library: the FIXatdl 1.1 XSD is FIX Protocol Limited's
    /// licensed schema (internal-use-only, no redistribution/derivative-works rights), so it cannot be
    /// vendored into this open-source repo. Callers who hold their own licensed copy of the FPL schema
    /// files, or who maintain an independently-authored XSD covering the constructs they care about, can
    /// load it into an <see cref="XmlSchemaSet"/> and pass it here. When null (default), no schema
    /// validation is performed and behaviour is unchanged.</param>
    /// <param name="customParameterTypes">Optional host-supplied custom (vendor-extension) <c>Parameter</c>
    /// <c>xsi:type</c> registrations - FIXatdl 1.1 permits a vendor to define a <c>Parameter</c> type
    /// beyond the standard set, with vendor-specific semantics this library cannot itself provide. Each
    /// registered <see cref="CustomParameterType"/> is looked up by its bare <c>xsi:type</c> name and used
    /// directly, so its CLR type may live in the caller's own assembly. When null (default), only the
    /// standard FIXatdl 1.1 parameter types are recognised (unchanged behaviour).</param>
    public StrategiesReader(
        ILoggerFactory? loggerFactory = null,
        IClock? clock = null,
        XmlSchemaSet? schemaSet = null,
        IReadOnlyList<CustomParameterType>? customParameterTypes = null
    )
    {
        _loggerFactory = loggerFactory ?? NullLoggerFactory.Instance;
        _log = _loggerFactory.CreateLogger<StrategiesReader>();
        _clock = clock ?? SystemClock.Instance;

        // Compile eagerly, up-front: XDocument.Validate() compiles an uncompiled schema set lazily on
        // first use, and XmlSchemaSet's internal compilation is not documented thread-safe. Compiling
        // here means concurrent Load() calls on this instance (already supported - see the per-call
        // strategyLoadedCount below) never race on that lazy compilation.
        if (schemaSet != null && !schemaSet.IsCompiled)
        {
            schemaSet.Compile();
        }

        _schemaSet = schemaSet;

        // Built once per reader instance, never touching the shared static SchemaDefinitions tree, so
        // one reader's custom types cannot leak into another reader's parse.
        _customParameterTypes = BuildCustomParameterTypeMap(customParameterTypes);
    }

    // Hardened reader settings shared by both Load overloads: prohibit DTD processing and use no
    // external resolver (defence-in-depth against XXE / external-entity expansion).
    private static readonly XmlReaderSettings _readerSettings = new()
    {
        DtdProcessing = DtdProcessing.Prohibit,
        XmlResolver = null,
    };

    /// <summary>
    /// Occurs when a strategy has been deserialized. This is a pre-resolution progress signal: it is
    /// raised as each strategy is deserialized, before cross-reference resolution (ResolveAll), which
    /// may still fail. Treat it as "deserialized", not "fully loaded and validated".
    /// </summary>
    public event EventHandler<StrategyLoadedEventArgs>? StrategyLoaded;

    /// <summary>
    /// Loads and deserializes strategies from a file path.
    /// </summary>
    /// <param name="path">The path to the FIXatdl XML file.</param>
    /// <returns>The deserialized strategies collection.</returns>
    public Strategies_t Load(string path)
    {
        if (_log.IsEnabled(LogLevel.Debug))
        {
            _log.LogDebug("Attempting to load strategies from file '{Path}'.", path);
        }

        XDocument document;

        using (XmlReader reader = XmlReader.Create(path, _readerSettings))
        {
            document = XDocument.Load(reader, LoadOptions.SetLineInfo | LoadOptions.PreserveWhitespace);
        }

        Strategies_t strategies = LoadStrategies(document);

        if (_log.IsEnabled(LogLevel.Debug))
        {
            _log.LogDebug("{Count} strategies loaded from file '{Path}'.", strategies.Count, path);
        }

        return strategies;
    }

    /// <summary>
    /// Loads and deserializes strategies from a stream.
    /// </summary>
    /// <param name="stream">The stream containing FIXatdl XML.</param>
    /// <returns>The deserialized strategies collection.</returns>
    public Strategies_t Load(Stream stream)
    {
        if (_log.IsEnabled(LogLevel.Debug))
        {
            _log.LogDebug("Attempting to load strategies from stream.");
        }

        XDocument document;

        using (XmlReader reader = XmlReader.Create(stream, _readerSettings))
        {
            document = XDocument.Load(reader, LoadOptions.SetLineInfo | LoadOptions.PreserveWhitespace);
        }

        Strategies_t strategies = LoadStrategies(document);

        if (_log.IsEnabled(LogLevel.Debug))
        {
            _log.LogDebug("{Count} strategies loaded from stream.", strategies.Count);
        }

        return strategies;
    }

    private Strategies_t LoadStrategies(XDocument document)
    {
        XElement? element = document.Element(AtdlNamespaces.core + "Strategies");

        if (element == null)
        {
            throw ThrowHelper.New<FixAtdlException>(this, ErrorMessages.StrategiesLoadFailure);
        }

        if (element.Descendants(AtdlNamespaces.core + "RepeatingGroup").Any())
        {
            throw ThrowHelper.New<FixAtdlException>(this, "RepeatingGroup elements are not supported.");
        }

        if (_schemaSet != null)
        {
            ValidateAgainstSchema(document);
        }

        ElementFactory factory = new(
            SchemaDefinitions.Strategies_t,
            typeof(Strategy_t),
            _loggerFactory,
            _customParameterTypes
        );

        // Counter is local to this call (not instance state), so concurrent Load calls on the same
        // StrategiesReader instance cannot corrupt each other's StrategyLoadedEventArgs.Index.
        int strategyLoadedCount = 0;

        // Index is a running count of strategies as they deserialize. Total is reported as 0 ("unknown")
        // because the full count is not known until deserialization completes (this fires mid-parse).
        void OnStrategyDeserialized(object? sender, ClassDeserializedEventArgs args) =>
            NotifyStrategyLoaded(strategyLoadedCount++, 0, (args.ExtraInfo as Strategy_t)!.Name);

        factory.ClassDeserialized += OnStrategyDeserialized;

        Strategies_t strategies;

        try
        {
            strategies = (Strategies_t)factory.DeserializeElement(element);
        }
        finally
        {
            // Unsubscribe so the handler does not linger if the factory is ever retained.
            factory.ClassDeserialized -= OnStrategyDeserialized;
        }

        strategies.ResolveAll();

        // Wire the injected clock into every Clock_t. Clock_t reads its clock lazily (at LoadInitValue
        // and value-set time, not during parse), so assigning here — after parse, before the host loads
        // defaults — lands the injected "now" without the caller walking the control graph by hand.
        foreach (Strategy_t strategy in strategies)
        {
            foreach (Clock_t clock in strategy.Controls.OfType<Clock_t>())
            {
                clock.Clock = _clock;
            }
        }

        return strategies;
    }

    private static Dictionary<string, CustomParameterType> BuildCustomParameterTypeMap(
        IReadOnlyList<CustomParameterType>? customParameterTypes
    )
    {
        Dictionary<string, CustomParameterType> map = new(StringComparer.Ordinal);

        CustomParameterType? duplicate = (customParameterTypes ?? []).FirstOrDefault(t =>
            !map.TryAdd(t.XsiTypeName, t)
        );

        if (duplicate != null)
        {
            throw new ArgumentException(
                $"Duplicate custom Parameter xsi:type registration: '{duplicate.XsiTypeName}'.",
                nameof(customParameterTypes)
            );
        }

        return map;
    }

    private void ValidateAgainstSchema(XDocument document)
    {
        List<string> errors = [];

        document.Validate(_schemaSet!, (_, e) => errors.Add(e.Exception?.Message ?? e.Message), addSchemaInfo: false);

        if (errors.Count > 0)
        {
            throw new SchemaValidationException(errors);
        }
    }

    private void NotifyStrategyLoaded(int index, int total, string strategyName)
    {
        StrategyLoaded?.Invoke(this, new StrategyLoadedEventArgs(index, total, strategyName));
    }
}

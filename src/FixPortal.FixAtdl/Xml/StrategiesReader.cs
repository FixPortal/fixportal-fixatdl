#region Copyright (c) 2010-2011, Steve Wilkinson (author)
//
//   This software is released under the MIT License..
//
#endregion

using Atdl4net.Diagnostics;
using Atdl4net.Diagnostics.Exceptions;
using Atdl4net.Model.Elements;
using Atdl4net.Resources;
using Atdl4net.Xml.Serialization;
using System.IO;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

#if !NET_40
using System.Xml;
#endif

namespace Atdl4net.Xml
{
    /// <summary>
    /// Reads and deserializes ATDL strategy definitions from an XML document or stream.
    /// Raises <see cref="StrategyLoaded"/> for each strategy as it is deserialized.
    /// </summary>
    public class StrategiesReader
    {
        // FP Enhancement: 2026-05-23 — TODO wire injected logger when refactoring class to accept ILogger.
        private readonly ILogger _log = NullLogger.Instance;

        public event System.EventHandler<StrategyLoadedEventArgs> StrategyLoaded;

        public Strategies_t Load(string path)
        {
            _log.LogDebug("Attempting to load strategies from file '{Path}'.", path);

            XDocument document = XDocument.Load(path, LoadOptions.SetLineInfo | LoadOptions.PreserveWhitespace);

            Strategies_t strategies = LoadStrategies(document);

            _log.LogDebug("{Count} strategies loaded from file '{Path}'.", strategies.Count, path);

            return strategies;
        }

        public Strategies_t Load(Stream stream)
        {
            _log.LogDebug("Attempting to load strategies from stream.");

            XDocument document;
#if NET_40
            document = XDocument.Load(stream, LoadOptions.SetLineInfo | LoadOptions.PreserveWhitespace);
#else
            using (XmlReader reader = XmlReader.Create(stream))
            {
                document = XDocument.Load(reader, LoadOptions.SetLineInfo | LoadOptions.PreserveWhitespace);
            }
#endif
            Strategies_t strategies = LoadStrategies(document);

            _log.LogDebug("{Count} strategies loaded from stream.", strategies.Count);

            return strategies;
        }

        private Strategies_t LoadStrategies(XDocument document)
        {
            XElement element = document.Element(AtdlNamespaces.core + "Strategies");

            if (element == null)
                throw ThrowHelper.New<Atdl4netException>(this, ErrorMessages.StrategiesLoadFailure);

            ElementFactory factory = new ElementFactory(SchemaDefinitions.Strategies_t, typeof(Strategy_t));

            factory.ClassDeserialized += new System.EventHandler<ClassDeserializedEventArgs>(OnStrategyDeserialized);

            Strategies_t strategies = (Strategies_t)factory.DeserializeElement(element);

            strategies.ResolveAll();

            return strategies;
        }

        private void OnStrategyDeserialized(object? sender, ClassDeserializedEventArgs args)
        {
            NotifyStrategyLoaded(0, 0, (args.ExtraInfo as Strategy_t)!.Name!); // FP Enhancement: 2026-05-23 — nullable cleanup deferred to Phase C.
        }

        private void NotifyStrategyLoaded(int index, int total, string strategyName)
        {
            StrategyLoaded?.Invoke(this, new StrategyLoadedEventArgs(index, total, strategyName));
        }
    }
}

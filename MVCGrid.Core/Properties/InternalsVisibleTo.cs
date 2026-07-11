using System.Runtime.CompilerServices;

// The net48 web adapter is a separate assembly but is a first-party part of MVCGrid.
// It needs access to internal glue (QueryStringParser, MVCGridHtmlGenerator,
// GridContext's internal GridDefinition setter, GridEngine.GetRenderingEngineInternal)
// without those becoming part of the public API surface.
[assembly: InternalsVisibleTo("MVCGrid.MvcWeb")]

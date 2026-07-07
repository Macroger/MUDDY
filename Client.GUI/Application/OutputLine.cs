// @file      OutputLine.cs
// @namespace Client.GUI.Application
// @brief     Represents a single line of game output text and its display colour.

namespace Client.GUI.Application;

/// <summary>
/// Represents a single line of game output, pairing display text with an ARGB hex colour code.
/// </summary>
/// <param name="Text">The text content of the output line.</param>
/// <param name="ColorHex">The ARGB hex colour string used to render the text. Defaults to opaque white.</param>
public record OutputLine(string Text = "", string ColorHex = "#FFFFFFFF");
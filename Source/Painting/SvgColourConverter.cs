﻿using System;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.Text;
using Svg.Helpers;

namespace Svg
{
    /// <summary>
    /// Converts string representations of colours into <see cref="Color"/> objects.
    /// </summary>
    public class SvgColourConverter : ColorConverter
    {
        private static readonly char[] SplitChars = new char[] { ',' , ' ' };

        private static int ClampInt(int value, int min, int max)
        {
            return Math.Min(Math.Max(value, min), max);
        }

        private static float ClampFloat(float value, float min, float max)
        {
            return Math.Min(Math.Max(value, min), max);
        }

        private static double ClampDouble(double value, double min, double max)
        {
            return Math.Min(Math.Max(value, min), max);
        }

        private static double ClampHue(double h)
        {
            if (h > 360.0)
            {
                h = h % 360.0;
            }

            if (h < 0)
            {
                h = 360.0 - (-h) % 360.0;
            }

            return h;
        }

        public object Parse(ITypeDescriptorContext context, CultureInfo culture, string colour)
        {
            var span = colour.AsSpan().Trim();

            // RGB support
            if (span.IndexOf("rgb".AsSpan()) == 0)
            {
                try
                {
                    // get the values from the RGB string
                    var start = span.IndexOf('(') + 1;
                    var length = span.IndexOf(')') - start;
                    var parts = new StringSplitEnumerator(span.Slice(start, length), SplitChars.AsSpan());
                    var count = 0;
                    int alpha = 255;
                    int red = default;
                    int green = default;
                    int blue = default;
                    bool isDecimal = false;

                    foreach (var part in parts)
                    {
                        var partValue = part.Value;
                        if (count == 0)
                        {
                            if (partValue.IndexOf('%') == partValue.Length - 1)
                            {
                                partValue = partValue.TrimEnd('%');
                                var redDecimal = StringParser.ToFloatAny(ref partValue);
                                redDecimal = ClampFloat(redDecimal, 0f, 100f);
                                red = (int) Math.Round(255 * redDecimal / 100f);
                                isDecimal = true;
                            }
                            else
                            {
                                red = StringParser.ToInt(ref partValue);
                                red = ClampInt(red, 0, 255);
                                isDecimal = false;
                            }
                        }
                        else if (count == 1)
                        {
                            if (partValue.IndexOf('%') == partValue.Length - 1)
                            {
                                if (!isDecimal)
                                {
                                    throw new SvgException("Colour is in an invalid format: '" + span.ToString() + "'");
                                }

                                partValue = partValue.TrimEnd('%');
                                var greenDecimal = StringParser.ToFloatAny(ref partValue);
                                greenDecimal = ClampFloat(greenDecimal, 0f, 100f);
                                green = (int) Math.Round(255 * greenDecimal / 100f);
                            }
                            else
                            {
                                if (isDecimal)
                                {
                                    throw new SvgException("Colour is in an invalid format: '" + span.ToString() + "'");
                                }

                                green = StringParser.ToInt(ref partValue);
                                green = ClampInt(green, 0, 255);
                            }
                        }
                        else if (count == 2)
                        {
                            if (partValue.IndexOf('%') == partValue.Length - 1)
                            {
                                if (!isDecimal)
                                {
                                    throw new SvgException("Colour is in an invalid format: '" + span.ToString() + "'");
                                }

                                partValue = partValue.TrimEnd('%');
                                var blueDecimal = StringParser.ToFloatAny(ref partValue);
                                blueDecimal = ClampFloat(blueDecimal, 0f, 100f);
                                blue = (int) Math.Round(255 * blueDecimal / 100f);
                            }
                            else
                            {
                                if (isDecimal)
                                {
                                    throw new SvgException("Colour is in an invalid format: '" + span.ToString() + "'");
                                }

                                blue = StringParser.ToInt(ref partValue);
                                blue = ClampInt(blue, 0, 255);
                            }
                        }
                        else if (count == 3)
                        {
                            // determine the alpha value if this is an RGBA (it will be the 4th value if there is one)
                            // the alpha portion of the rgba is not an int 0-255 it is a decimal between 0 and 1
                            // so we have to determine the corresponding byte value
                            if (partValue.IndexOf('.') == 0)
                            {
                                // TODO: partValue = "0" + partValue;
                            }

                            var alphaDecimal = StringParser.ToDouble(ref partValue);
                            if (alphaDecimal <= 1)
                            {
                                alphaDecimal = ClampDouble(alphaDecimal, 0f, 1f);
                                alpha = (int) Math.Round(alphaDecimal * 255);
                            }
                            else
                            {
                                alphaDecimal = ClampDouble(alphaDecimal, 0f, 255f);
                                alpha = (int) Math.Round(alphaDecimal);
                            }
                        }
                        else
                        {
                            throw new SvgException("Colour is in an invalid format: '" + span.ToString() + "'");
                        }

                        count++;
                    }

                    var color = Color.FromArgb(alpha, red, green, blue);
                    return color;
                }
                catch
                {
                    throw new SvgException("Colour is in an invalid format: '" + span.ToString() + "'");
                }
            }

            // HSL support
            if (span.IndexOf("hsl".AsSpan()) == 0)
            {
                try
                {
                    // get the values from the HSL string
                    var start = span.IndexOf('(') + 1;
                    var length = span.IndexOf(')') - start;
                    var parts = new StringSplitEnumerator(span.Slice(start, length), SplitChars.AsSpan());
                    var count = 0;
                    var h = default(double);
                    var s = default(double);
                    var l = default(double);

                    // Get the HSL values in a range from 0 to 1.
                    foreach (var part in parts)
                    {
                        var partValue = part.Value;
                        if (count == 0)
                        {
                            var hDecimal = StringParser.ToDouble(ref partValue);
                            hDecimal = ClampHue(hDecimal);
                            h = hDecimal / 360.0;
                        }
                        else if (count == 1)
                        {
                            if (partValue.IndexOf('%') == partValue.Length - 1)
                            {
                                partValue = partValue.TrimEnd('%');

                                var sDecimal = StringParser.ToDouble(ref partValue);
                                sDecimal = ClampDouble(sDecimal, 0f, 100f);
                                s = sDecimal / 100.0;
                            }
                            else
                            {
                                throw new SvgException("Colour is in an invalid format: '" + span.ToString() + "'");
                            }
                        }
                        else if (count == 2)
                        {
                            if (partValue.IndexOf('%') == partValue.Length - 1)
                            {
                                partValue = partValue.TrimEnd('%');
                                var lDecimal = StringParser.ToDouble(ref partValue);
                                lDecimal = ClampDouble(lDecimal, 0f, 100f);
                                l = lDecimal / 100.0;
                            }
                            else
                            {
                                throw new SvgException("Colour is in an invalid format: '" + span.ToString() + "'");
                            }
                        }
                        else
                        {
                            throw new SvgException("Colour is in an invalid format: '" + span.ToString() + "'");
                        }

                        count++;
                    }

                    // Convert the HSL color to an RGB color
                    var color = Hsl2Rgb(h, s, l);
                    return color;
                }
                catch
                {
                    throw new SvgException("Colour is in an invalid format: '" + span.ToString() + "'");
                }
            }

            // HEX support
            if (span.IndexOf('#') == 0)
            {
                // The format of an RGB value in hexadecimal notation is a '#' immediately followed by either three or six hexadecimal characters.

                if (span.Length == 4)
                {
                    // The three-digit RGB notation (#rgb) is converted into six-digit form (#rrggbb) by replicating digits, not by adding zeros.
                    // For example, #fb0 expands to #ffbb00.
                    Span<char> redString = stackalloc char[2] {span[1], span[1]};
                    Span<char> greenString = stackalloc char[2] {span[2], span[2]};
                    Span<char> blueString = stackalloc char[2] {span[3], span[3]};
#if NETSTANDARD2_1 || NETCORE || NETCOREAPP2_1 || NETCOREAPP3_1 || NET5_0
                    var red = int.Parse(redString, NumberStyles.AllowHexSpecifier);
                    var green = int.Parse(greenString, NumberStyles.AllowHexSpecifier);
                    var blue = int.Parse(blueString, NumberStyles.AllowHexSpecifier);
#else
                    var red = int.Parse(redString.ToString(), NumberStyles.AllowHexSpecifier);
                    var green = int.Parse(greenString.ToString(), NumberStyles.AllowHexSpecifier);
                    var blue = int.Parse(blueString.ToString(), NumberStyles.AllowHexSpecifier);
#endif
                    return Color.FromArgb(255, red, green, blue);
                }

                if (span.Length == 7)
                {
                    var redString = span.Slice(1, 2);
                    var greenString = span.Slice(3, 2);
                    var blueString = span.Slice(5, 2);
#if NETSTANDARD2_1 || NETCORE || NETCOREAPP2_1 || NETCOREAPP3_1 || NET5_0
                    var red = int.Parse(redString, NumberStyles.AllowHexSpecifier);
                    var green = int.Parse(greenString, NumberStyles.AllowHexSpecifier);
                    var blue = int.Parse(blueString, NumberStyles.AllowHexSpecifier);
#else
                    var red = int.Parse(redString.ToString(), NumberStyles.AllowHexSpecifier);
                    var green = int.Parse(greenString.ToString(), NumberStyles.AllowHexSpecifier);
                    var blue = int.Parse(blueString.ToString(), NumberStyles.AllowHexSpecifier);
#endif
                    return Color.FromArgb(255, red, green, blue);
                }

                // TODO: Check if there are other supported formats.
                // var hex = string.Format(culture, "#{0}{0}{1}{1}{2}{2}", colour[1], colour[2], colour[3]);
                // return base.ConvertFrom(context, culture, hex);
            }

            // SystemColors support
            if (TryToGetSystemColor(ref span, out var systemColor))
            {
                return systemColor;
            }

            // Numbers are handled as colors by System.Drawing.ColorConverter - we
            // have to prevent this and ignore the color instead (see #342).
#if NETSTANDARD2_1 || NETCORE || NETCOREAPP2_1 || NETCOREAPP3_1 || NET5_0
            if (int.TryParse(span, NumberStyles.Integer, CultureInfo.InvariantCulture, out _))
            {
                return SvgPaintServer.NotSet;
            }
#else
            if (int.TryParse(span.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out _))
            {
                return SvgPaintServer.NotSet;
            }
#endif

            // Support for grey colors
            var hasGrey = span.Contains("grey".AsSpan(), StringComparison.InvariantCultureIgnoreCase);
            if (hasGrey)
            {
                Span<char> lowerInvariant = stackalloc char[32];
                var grey = "grey".AsSpan();
                span.ToLowerInvariant(lowerInvariant);

                var index = lowerInvariant.IndexOf(grey);
                if (index >= 0 && index + 4 == span.Length)
                {
                    if (span[index] == 'G')
                    {
                        var gray = $"{span.Slice(0, index).ToString()}Gray";
                        return base.ConvertFrom(context, culture, gray);
                    }
                    else
                    {
                        var gray = $"{span.Slice(0, index).ToString()}gray";
                        return base.ConvertFrom(context, culture, gray);
                    }
                }
            }

            // TODO: Optimize ToString()
            return base.ConvertFrom(context, culture, colour);
        }

        private static uint ComputeStringHash(in ReadOnlySpan<char> text)
        {
            uint hashCode = 0;
            if (text != null)
            {
                var length = text.Length;

                hashCode = unchecked((uint)2166136261);

                int i = 0;
                goto start;

                again:
                hashCode = unchecked((text[i] ^ hashCode) * 16777619);
                i = i + 1;

                start:
                if (i < length)
                    goto again;
            }
            return hashCode;
        }

        public static void ToLowerAscii(in ReadOnlySpan<char> colour, ref Span<char> buffer)
        {
            for (int i = 0; i < colour.Length; i++)
            {
                var c = colour[i];
                switch (c)
                {
                    case 'A': buffer[i] = 'a'; break;
                    case 'B': buffer[i] = 'b'; break;
                    case 'C': buffer[i] = 'c'; break;
                    case 'D': buffer[i] = 'd'; break;
                    case 'E': buffer[i] = 'e'; break;
                    case 'F': buffer[i] = 'f'; break;
                    case 'G': buffer[i] = 'g'; break;
                    case 'H': buffer[i] = 'h'; break;
                    case 'I': buffer[i] = 'i'; break;
                    case 'J': buffer[i] = 'j'; break;
                    case 'K': buffer[i] = 'k'; break;
                    case 'L': buffer[i] = 'l'; break;
                    case 'M': buffer[i] = 'm'; break;
                    case 'N': buffer[i] = 'n'; break;
                    case 'O': buffer[i] = 'o'; break;
                    case 'P': buffer[i] = 'p'; break;
                    case 'Q': buffer[i] = 'q'; break;
                    case 'R': buffer[i] = 'r'; break;
                    case 'S': buffer[i] = 's'; break;
                    case 'T': buffer[i] = 't'; break;
                    case 'U': buffer[i] = 'u'; break;
                    case 'V': buffer[i] = 'v'; break;
                    case 'W': buffer[i] = 'w'; break;
                    case 'X': buffer[i] = 'x'; break;
                    case 'Y': buffer[i] = 'y'; break;
                    case 'Z': buffer[i] = 'z'; break;
                    default: buffer[i] = c; break;
                }
            }
        }

        public static bool TryToGetSystemColor(ref ReadOnlySpan<char> colour, out Color systemColor)
        {
            Span<char> buffer = stackalloc char[colour.Length];
            ToLowerAscii(colour, ref buffer);
            var stringHash = ComputeStringHash(buffer);
            switch(stringHash)
            {
                case 0x96b1f469: systemColor = SystemColors.ActiveBorder; return true; // activeborder
                case 0x2cc5885f: systemColor = SystemColors.ActiveCaption; return true; // activecaption
                case 0xb4f0f429: systemColor = SystemColors.AppWorkspace; return true; // appworkspace
                case 0x4babd89d: systemColor = SystemColors.Desktop; return true; // background
                case 0xb8a66038: systemColor = SystemColors.ButtonFace; return true; // buttonface
                case 0x9050ce9b: systemColor = SystemColors.ControlLightLight; return true; // buttonhighlight
                case 0x6ceea1b5: systemColor = SystemColors.ControlDark; return true; // buttonshadow
                case 0xb6b04242: systemColor = SystemColors.ControlText; return true; // buttontext
                case 0xf29413de: systemColor = SystemColors.ActiveCaptionText; return true; // captiontext
                case 0x9642ba91: systemColor = SystemColors.GrayText; return true; // graytext
                case 0x1c9ff127: systemColor = SystemColors.Highlight; return true; // highlight
                case 0x635b6be0: systemColor = SystemColors.HighlightText; return true; // highlighttext
                case 0xa59d8bc6: systemColor = SystemColors.InactiveBorder; return true; // inactiveborder
                case 0xba88bd1a: systemColor = SystemColors.InactiveCaption; return true; // inactivecaption
                case 0xcb67dc11: systemColor = SystemColors.InactiveCaptionText; return true; // inactivecaptiontext
                case 0x9c8c4bdd: systemColor = SystemColors.Info; return true; // infobackground
                case 0xb164837e: systemColor = SystemColors.InfoText; return true; // infotext
                case 0x99e4dd3a: systemColor = SystemColors.Menu; return true; // menu
                case 0x4c924831: systemColor = SystemColors.MenuText; return true; // menutext
                case 0xd5b6c079: systemColor = SystemColors.ScrollBar; return true; // scrollbar
                case 0xffa62901: systemColor = SystemColors.ControlDarkDark; return true; // threeddarkshadow
                case 0x77fd6efc: systemColor = SystemColors.Control; return true; // threedface
                case 0xd4724bc7: systemColor = SystemColors.ControlLight; return true; // threedhighlight
                case 0x238bb757: systemColor = SystemColors.ControlLightLight; return true; // threedlightshadow
                case 0xa172b7dd: systemColor = SystemColors.Window; return true; // window
                case 0x6f554c7e: systemColor = SystemColors.WindowFrame; return true; // windowframe
                case 0xe477b746: systemColor = SystemColors.WindowText; return true; // windowtext
            }
            systemColor = default;
            return false;
        }

        /// <summary>
        /// Converts the given object to the converter's native type.
        /// </summary>
        /// <param name="context">A <see cref="T:System.ComponentModel.TypeDescriptor"/> that provides a format context. You can use this object to get additional information about the environment from which this converter is being invoked.</param>
        /// <param name="culture">A <see cref="T:System.Globalization.CultureInfo"/> that specifies the culture to represent the color.</param>
        /// <param name="value">The object to convert.</param>
        /// <returns>
        /// An <see cref="T:System.Object"/> representing the converted value.
        /// </returns>
        /// <exception cref="T:System.ArgumentException">The conversion cannot be performed.</exception>
        /// <PermissionSet>
        ///     <IPermission class="System.Security.Permissions.FileIOPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true"/>
        /// </PermissionSet>
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            if (value is string colour)
            {
                return Parse(context, culture, colour);
            }

            return base.ConvertFrom(context, culture, value);
        }

        public object Parse_OLD(ITypeDescriptorContext context, CultureInfo culture, string colour)
        {
            colour = colour.Trim();

            // RGB support
            if (colour.StartsWith("rgb", StringComparison.InvariantCulture))
            {
                try
                {
                    int start = colour.IndexOf("(", StringComparison.InvariantCulture) + 1;

                    //get the values from the RGB string
                    string[] values = colour.Substring(start, colour.IndexOf(")", StringComparison.InvariantCulture) - start)
                        .Split(new char[] {',', ' '}, StringSplitOptions.RemoveEmptyEntries);

                    //determine the alpha value if this is an RGBA (it will be the 4th value if there is one)
                    int alphaValue = 255;
                    if (values.Length > 3)
                    {
                        //the alpha portion of the rgba is not an int 0-255 it is a decimal between 0 and 1
                        //so we have to determine the corosponding byte value
                        var alphastring = values[3];
                        if (alphastring.StartsWith(".", StringComparison.InvariantCulture))
                        {
                            alphastring = "0" + alphastring;
                        }

                        var alphaDecimal = decimal.Parse(alphastring, CultureInfo.InvariantCulture);

                        if (alphaDecimal <= 1)
                        {
                            alphaValue = (int) Math.Round(alphaDecimal * 255);
                        }
                        else
                        {
                            alphaValue = (int) Math.Round(alphaDecimal);
                        }
                    }

                    Color colorpart;
                    if (values[0].Trim().EndsWith("%", StringComparison.InvariantCulture))
                    {
                        colorpart = Color.FromArgb(alphaValue,
                            (int) Math.Round(255 * float.Parse(values[0].Trim().TrimEnd('%'), NumberStyles.Any,
                                CultureInfo.InvariantCulture) / 100f),
                            (int) Math.Round(255 * float.Parse(values[1].Trim().TrimEnd('%'), NumberStyles.Any,
                                CultureInfo.InvariantCulture) / 100f),
                            (int) Math.Round(255 * float.Parse(values[2].Trim().TrimEnd('%'), NumberStyles.Any,
                                CultureInfo.InvariantCulture) / 100f));
                    }
                    else
                    {
                        colorpart = Color.FromArgb(alphaValue, int.Parse(values[0], CultureInfo.InvariantCulture),
                            int.Parse(values[1], CultureInfo.InvariantCulture),
                            int.Parse(values[2], CultureInfo.InvariantCulture));
                    }

                    return colorpart;
                }
                catch
                {
                    throw new SvgException("Colour is in an invalid format: '" + colour + "'");
                }
            }

            // HSL support
            if (colour.StartsWith("hsl", StringComparison.InvariantCulture))
            {
                try
                {
                    int start = colour.IndexOf("(", StringComparison.InvariantCulture) + 1;

                    //get the values from the RGB string
                    string[] values = colour.Substring(start, colour.IndexOf(")", StringComparison.InvariantCulture) - start)
                        .Split(new char[] {',', ' '}, StringSplitOptions.RemoveEmptyEntries);
                    if (values[1].EndsWith("%", StringComparison.InvariantCulture))
                    {
                        values[1] = values[1].TrimEnd('%');
                    }

                    if (values[2].EndsWith("%", StringComparison.InvariantCulture))
                    {
                        values[2] = values[2].TrimEnd('%');
                    }

                    // Get the HSL values in a range from 0 to 1.
                    double h = double.Parse(values[0], CultureInfo.InvariantCulture) / 360.0;
                    double s = double.Parse(values[1], CultureInfo.InvariantCulture) / 100.0;
                    double l = double.Parse(values[2], CultureInfo.InvariantCulture) / 100.0;
                    // Convert the HSL color to an RGB color
                    Color colorpart = Hsl2Rgb(h, s, l);
                    return colorpart;
                }
                catch
                {
                    throw new SvgException("Colour is in an invalid format: '" + colour + "'");
                }
            }

            // HEX support
            if (colour.StartsWith("#", StringComparison.InvariantCulture) && colour.Length == 4)
            {
                colour = string.Format(culture, "#{0}{0}{1}{1}{2}{2}", colour[1], colour[2], colour[3]);
                return base.ConvertFrom(context, culture, colour);
            }

            // SystemColors support
            if (TryToGetSystemColor_OLD(colour, out var systemColor))
            {
                return systemColor;
            }

            // Numbers are handled as colors by System.Drawing.ColorConverter - we
            // have to prevent this and ignore the color instead (see #342).
            if (int.TryParse(colour, NumberStyles.Integer, CultureInfo.InvariantCulture, out _))
            {
                return SvgPaintServer.NotSet;
            }

            var index = colour.LastIndexOf("grey", StringComparison.InvariantCultureIgnoreCase);
            if (index >= 0 && index + 4 == colour.Length)
            {
                var gray = new StringBuilder(colour)
                    .Replace("grey", "gray", index, 4)
                    .Replace("Grey", "Gray", index, 4)
                    .ToString();

                return base.ConvertFrom(context, culture, gray);
            }

            return base.ConvertFrom(context, culture, colour);
        }

        public static bool TryToGetSystemColor_OLD(string colour, out Color systemColor)
        {
            switch (colour.ToLowerInvariant())
            {
                case "activeborder":
                {
                    systemColor = SystemColors.ActiveBorder;
                    return true;
                }
                case "activecaption":
                {
                    systemColor = SystemColors.ActiveCaption;
                    return true;
                }
                case "appworkspace":
                {
                    systemColor = SystemColors.AppWorkspace;
                    return true;
                }
                case "background":
                {
                    systemColor = SystemColors.Desktop;
                    return true;
                }
                case "buttonface":
                {
                    systemColor = SystemColors.Control;
                    return true;
                }
                case "buttonhighlight":
                {
                    systemColor = SystemColors.ControlLightLight;
                    return true;
                }
                case "buttonshadow":
                {
                    systemColor = SystemColors.ControlDark;
                    return true;
                }
                case "buttontext":
                {
                    systemColor = SystemColors.ControlText;
                    return true;
                }
                case "captiontext":
                {
                    systemColor = SystemColors.ActiveCaptionText;
                    return true;
                }
                case "graytext":
                {
                    systemColor = SystemColors.GrayText;
                    return true;
                }
                case "highlight":
                {
                    systemColor = SystemColors.Highlight;
                    return true;
                }
                case "highlighttext":
                {
                    systemColor = SystemColors.HighlightText;
                    return true;
                }
                case "inactiveborder":
                {
                    systemColor = SystemColors.InactiveBorder;
                    return true;
                }
                case "inactivecaption":
                {
                    systemColor = SystemColors.InactiveCaption;
                    return true;
                }
                case "inactivecaptiontext":
                {
                    systemColor = SystemColors.InactiveCaptionText;
                    return true;
                }
                case "infobackground":
                {
                    systemColor = SystemColors.Info;
                    return true;
                }
                case "infotext":
                {
                    systemColor = SystemColors.InfoText;
                    return true;
                }
                case "menu":
                {
                    systemColor = SystemColors.Menu;
                    return true;
                }
                case "menutext":
                {
                    systemColor = SystemColors.MenuText;
                    return true;
                }
                case "scrollbar":
                {
                    systemColor = SystemColors.ScrollBar;
                    return true;
                }
                case "threeddarkshadow":
                {
                    systemColor = SystemColors.ControlDarkDark;
                    return true;
                }
                case "threedface":
                {
                    systemColor = SystemColors.Control;
                    return true;
                }
                case "threedhighlight":
                {
                    systemColor = SystemColors.ControlLight;
                    return true;
                }
                case "threedlightshadow":
                {
                    systemColor = SystemColors.ControlLightLight;
                    return true;
                }
                case "window":
                {
                    systemColor = SystemColors.Window;
                    return true;
                }
                case "windowframe":
                {
                    systemColor = SystemColors.WindowFrame;
                    return true;
                }
                case "windowtext":
                {
                    systemColor = SystemColors.WindowText;
                    return true;
                }
            }

            systemColor = default;
            return false;
        }

        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            if (sourceType == typeof(string))
            {
                return true;
            }

            return base.CanConvertFrom(context, sourceType);
        }

        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            if (destinationType == typeof(string))
            {
                return true;
            }

            return base.CanConvertTo(context, destinationType);
        }

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == typeof(string))
            {
                var colorString = ColorTranslator.ToHtml((Color)value).Replace("LightGrey", "LightGray");
                // color names are expected to be lower case in XML
                return colorString.StartsWith("#", StringComparison.InvariantCulture) ? colorString : colorString.ToLowerInvariant();
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }

        /// <summary>
        /// Converts HSL color (with HSL specified from 0 to 1) to RGB color.
        /// Taken from http://www.geekymonkey.com/Programming/CSharp/RGB2HSL_HSL2RGB.htm
        /// </summary>
        /// <param name="h"></param>
        /// <param name="sl"></param>
        /// <param name="l"></param>
        /// <returns></returns>
        private static Color Hsl2Rgb(double h, double sl, double l)
        {
            double r = l;   // default to gray
            double g = l;
            double b = l;
            double v = (l <= 0.5) ? (l * (1.0 + sl)) : (l + sl - l * sl);
            if (v > 0)
            {
                double m;
                double sv;
                int sextant;
                double fract, vsf, mid1, mid2;

                m = l + l - v;
                sv = (v - m) / v;
                h *= 6.0;
                sextant = (int)h;
                fract = h - sextant;
                vsf = v * sv * fract;
                mid1 = m + vsf;
                mid2 = v - vsf;
                switch (sextant)
                {
                    case 0:
                        r = v;
                        g = mid1;
                        b = m;
                        break;
                    case 1:
                        r = mid2;
                        g = v;
                        b = m;
                        break;
                    case 2:
                        r = m;
                        g = v;
                        b = mid1;
                        break;
                    case 3:
                        r = m;
                        g = mid2;
                        b = v;
                        break;
                    case 4:
                        r = mid1;
                        g = m;
                        b = v;
                        break;
                    case 5:
                        r = v;
                        g = m;
                        b = mid2;
                        break;
                }
            }
            Color rgb = Color.FromArgb((int)Math.Round(r * 255.0), (int)Math.Round(g * 255.0), (int)Math.Round(b * 255.0));
            return rgb;
        }
    }
}

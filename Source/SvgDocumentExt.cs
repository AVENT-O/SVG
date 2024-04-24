using ExCSS;
using Svg.Css;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml;
namespace Svg;


public partial class SvgDocumentExt : SvgDocument
{
    public Dictionary<string, SvgPath> SvgPathPitches { get; set; }
    public Dictionary<string, SvgEllipse> SvgPathPhotoLoc { get; set; }
    public Dictionary<string, SvgRectangle> SvgRectZones { get; set; }
    public Dictionary<string, SvgPath> SvgPathCamp { get; set; }


    /// <summary>
    /// OpenExts the document at the specified path and loads the SVG contents.
    /// </summary>
    /// <param name="path">A <see cref="string"/> containing the path of the file to open.</param>
    /// <returns>An <see cref="SvgDocumentExt"/> with the contents loaded.</returns>
    /// <exception cref="FileNotFoundException">The document at the specified <paramref name="path"/> cannot be found.</exception>
    public static SvgDocumentExt OpenExt(string path)
    {
        return OpenExt<SvgDocumentExt>(path, null);
    }


    /// <summary>
    /// OpenExts the document at the specified path and loads the SVG contents.
    /// </summary>
    /// <param name="path">A <see cref="string"/> containing the path of the file to open.</param>
    /// <returns>An <see cref="SvgDocumentExt"/> with the contents loaded.</returns>
    /// <exception cref="FileNotFoundException">The document at the specified <paramref name="path"/> cannot be found.</exception>
    public static T OpenExt<T>(string path) where T : SvgDocumentExt, new()
    {
        return OpenExt<T>(path, null);
    }


    /// <summary>
    /// OpenExts the document at the specified path and loads the SVG contents.
    /// </summary>
    /// <param name="path">A <see cref="string"/> containing the path of the file to open.</param>
    /// <param name="entities">A dictionary of custom entity definitions to be used when resolving XML entities within the document.</param>
    /// <returns>An <see cref="SvgDocumentExt"/> with the contents loaded.</returns>
    /// <exception cref="FileNotFoundException">The document at the specified <paramref name="path"/> cannot be found.</exception>
    public static T OpenExt<T>(string path, Dictionary<string, string> entities) where T : SvgDocumentExt, new()
    {
        if (string.IsNullOrEmpty(path))
        {
            throw new ArgumentNullException("path");
        }


        if (!File.Exists(path))
        {
            throw new FileNotFoundException("The specified document cannot be found.", path);
        }


        using (var stream = File.OpenRead(path))
        {
            var doc = OpenExt<T>(stream, entities);
            doc.BaseUri = new Uri(System.IO.Path.GetFullPath(path));
            return doc;
        }
    }


    /// <summary>
    /// Attempts to open an SVG document from the specified <see cref="Stream"/>.
    /// </summary>
    /// <param name="stream">The <see cref="Stream"/> containing the SVG document to open.</param>
    public static T OpenExt<T>(Stream stream) where T : SvgDocumentExt, new()
    {
        return OpenExt<T>(stream, null);
    }


    /// <summary>
    /// OpenExts an SVG document from the specified <see cref="Stream"/> and adds the specified entities.
    /// </summary>
    /// <param name="stream">The <see cref="Stream"/> containing the SVG document to open.</param>
    /// <param name="entities">Custom entity definitions.</param>
    /// <exception cref="ArgumentNullException">The <paramref name="stream"/> parameter cannot be <c>null</c>.</exception>
    public static T OpenExt<T>(Stream stream, Dictionary<string, string> entities) where T : SvgDocumentExt, new()
    {
        if (stream == null)
        {
            throw new ArgumentNullException("stream");
        }


        // Don't close the stream via a dispose: that is the client's job.
        var reader = new SvgTextReader(stream, entities)
        {
            XmlResolver = new SvgDtdResolver(),
            WhitespaceHandling = WhitespaceHandling.Significant,
            DtdProcessing = DisableDtdProcessing ? DtdProcessing.Ignore : DtdProcessing.Parse,
        };
        return CreateExt<T>(reader);
    }


    /// <summary>
    /// Attempts to create an SVG document from the specified string data.
    /// </summary>
    /// <param name="svg">The SVG data.</param>
    public static T FromSvgExt<T>(string svg) where T : SvgDocumentExt, new()
    {
        if (string.IsNullOrEmpty(svg))
        {
            throw new ArgumentNullException("svg");
        }


        using (var strReader = new StringReader(svg))
        {
            var reader = new SvgTextReader(strReader, null)
            {
                XmlResolver = new SvgDtdResolver(),
                WhitespaceHandling = WhitespaceHandling.Significant,
                DtdProcessing = DisableDtdProcessing ? DtdProcessing.Ignore : DtdProcessing.Parse,
            };
            return CreateExt<T>(reader);
        }
    }


    /// <summary>
    /// Attempts to open an SVG document from the specified <see cref="XmlReader"/>.
    /// </summary>
    /// <param name="reader">The <see cref="XmlReader"/> containing the SVG document to open.</param>
    public static T OpenExt<T>(XmlReader reader) where T : SvgDocumentExt, new()
    {
        if (reader == null)
        {
            throw new ArgumentNullException("reader");
        }


        using (var svgReader = XmlReader.Create(reader, new XmlReaderSettings()
        {
            XmlResolver = new SvgDtdResolver(),
            DtdProcessing = DtdProcessing.Parse,
        }))
        {
            return CreateExt<T>(svgReader);
        }
    }




    private static T CreateExt<T>(XmlReader reader) where T : SvgDocumentExt, new()
    {
        var styles = new List<ISvgNode>();
        var elementFactory = new SvgElementFactory();


        var svgDocument = CreateExt<T>(reader, elementFactory, styles);


        if (styles.Any())
        {
            var cssTotal = string.Join(Environment.NewLine, styles.Select(s => s.Content).ToArray());
            var stylesheetParser = new StylesheetParser(true, true, tolerateInvalidValues: true);
            var stylesheet = stylesheetParser.Parse(cssTotal);


            foreach (var rule in stylesheet.StyleRules)
                try
                {
                    var rootNode = new NonSvgElement();
                    rootNode.Children.Add(svgDocument);


                    var elemsToStyle = rootNode.QuerySelectorAll(rule.Selector, elementFactory);
                    foreach (var elem in elemsToStyle)
                        foreach (var declaration in rule.Style)
                        {
                            elem.AddStyle(declaration.Name, declaration.Original, rule.Selector.GetSpecificity());
                        }
                }
                catch (Exception ex)
                {
                    Trace.TraceWarning(ex.Message);
                }
        }
        svgDocument?.FlushStyles(true);


        svgDocument.SvgPathPitches = new();
        svgDocument.SvgPathPhotoLoc = new();
        svgDocument.SvgRectZones = new();
        svgDocument.SvgPathCamp = new();


        foreach (var svgElement in svgDocument.IdManager.IdValueMap)
        {
            if (svgElement.Value is SvgPath && svgElement.Key.StartsWith("PT"))
            {
                svgDocument.SvgPathPitches.Add(svgElement.Key, svgElement.Value as SvgPath);
            }
            else if (svgElement.Value is SvgEllipse && svgElement.Key.StartsWith("PL"))
            {
                svgDocument.SvgPathPhotoLoc.Add(svgElement.Key, svgElement.Value as SvgEllipse);
            }
            else if (svgElement.Value is SvgRectangle && svgElement.Key.StartsWith("PV"))
            {
                svgDocument.SvgRectZones.Add(svgElement.Key, svgElement.Value as SvgRectangle);
            }
            else if (svgElement.Value is SvgPath && svgElement.Key.StartsWith("PC"))
            {
                svgDocument.SvgPathCamp.Add(svgElement.Key, svgElement.Value as SvgPath);
            }
        }


        return svgDocument;
    }


    /// <summary> OpenExt Svg Document without applying Stylesheets. </summary>
    /// <typeparam name="T">SvgDocumentExt to create</typeparam>
    /// <param name="reader">reader</param>
    /// <param name="elementFactory">elementFactory</param>
    /// <param name="styles">read svg StyleSheets</param>
    /// <returns>CreateExtd Svg Document</returns>
    internal static T CreateExt<T>(XmlReader reader, SvgElementFactory elementFactory, List<ISvgNode> styles)
        where T : SvgDocumentExt, new()
    {
#if !NO_SDC
        if (!SkipGdiPlusCapabilityCheck)
        {
            EnsureSystemIsGdiPlusCapable(); // Validate whether the GDI+ can be loaded, this will yield an exception if not
        }
#endif
        var elementStack = new Stack<SvgElement>();
        bool elementEmpty;
        SvgElement element = null;
        SvgElement parent;
        T svgDocument = null;


        while (reader.Read())
        {
            try
            {
                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        // Does this element have a value or children
                        // (Must do this check here before we progress to another node)
                        elementEmpty = reader.IsEmptyElement;
                        // CreateExt element
                        if (elementStack.Count > 0)
                        {
                            element = elementFactory.CreateElement(reader, svgDocument);
                        }
                        else
                        {
                            svgDocument = elementFactory.CreateDocument<T>(reader);
                            element = svgDocument;
                        }


                        // Add to the parents children
                        if (elementStack.Count > 0)
                        {
                            parent = elementStack.Peek();
                            if (parent != null && element != null)
                            {
                                parent.Children.Add(element);
                                parent.Nodes.Add(element);
                            }
                        }


                        // Push element into stack
                        elementStack.Push(element);


                        // Need to process if the element is empty
                        if (elementEmpty)
                        {
                            goto case XmlNodeType.EndElement;
                        }


                        break;
                    case XmlNodeType.EndElement:


                        // Pop the element out of the stack
                        element = elementStack.Pop();


                        if (element.Nodes.OfType<SvgContentNode>().Any())
                        {
                            element.Content = string.Concat((from n in element.Nodes select n.Content).ToArray());
                        }
                        else
                        {
                            element.Nodes.Clear(); // No sense wasting the space where it isn't needed
                        }


                        var unknown = element as SvgUnknownElement;
                        if (unknown != null && unknown.ElementName == "style")
                        {
                            styles.Add(unknown);
                        }
                        break;
                    case XmlNodeType.CDATA:
                    case XmlNodeType.Text:
                    case XmlNodeType.SignificantWhitespace:
                        element = elementStack.Peek();
                        element.Nodes.Add(new SvgContentNode() { Content = reader.Value });
                        break;
                    case XmlNodeType.EntityReference:
                        reader.ResolveEntity();
                        element = elementStack.Peek();
                        element.Nodes.Add(new SvgContentNode() { Content = reader.Value });
                        break;
                }
            }
            catch (Exception exc)
            {
                Trace.TraceError(exc.Message);
            }
        }


        return svgDocument;
    }


    /// <summary>
    /// OpenExts an SVG document from the specified <see cref="XmlDocument"/>.
    /// </summary>
    /// <param name="document">The <see cref="XmlDocument"/> containing the SVG document XML.</param>
    /// <exception cref="ArgumentNullException">The <paramref name="document"/> parameter cannot be <c>null</c>.</exception>
    public static SvgDocumentExt OpenExt(XmlDocument document)
    {
        if (document == null)
        {
            throw new ArgumentNullException("document");
        }


        var reader = new SvgNodeReader(document.DocumentElement, null);
        return CreateExt<SvgDocumentExt>(reader);
    }
}

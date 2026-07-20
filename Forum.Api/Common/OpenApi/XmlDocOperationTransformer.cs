// Forum.Api/Common/OpenApi/XmlDocOperationTransformer.cs
using System.Reflection;
using System.Xml.Linq;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Models;

namespace Forum.Api.Common.OpenApi;

/// <summary>
/// Copies each action method's XML &lt;summary&gt; into the OpenAPI operation summary.
/// The built-in .NET 9 OpenAPI generator doesn't read the XML doc file on its own,
/// so without this the endpoint descriptions never reach Scalar. Summaries are read
/// once from the compiled assembly's .xml and cached by method.
/// </summary>
public sealed class XmlDocOperationTransformer : IOpenApiOperationTransformer
{
    private static readonly Dictionary<string, string> Summaries = LoadSummaries();

    public Task TransformAsync(
        OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken ct)
    {
        if (context.Description.ActionDescriptor is ControllerActionDescriptor action &&
            Summaries.TryGetValue(MemberKey(action.MethodInfo), out var summary) &&
            string.IsNullOrEmpty(operation.Summary))
        {
            operation.Summary = summary;
        }

        return Task.CompletedTask;
    }

    // Builds the "M:Namespace.Type.Method(argtypes)" doc-comment key for a method,
    // matching how <member name="..."> is written in the generated XML.
    private static string MemberKey(MethodInfo method)
    {
        var type = method.DeclaringType!;
        var parameters = method.GetParameters();
        var args = parameters.Length == 0
            ? string.Empty
            : "(" + string.Join(",", parameters.Select(p => TypeName(p.ParameterType))) + ")";
        return $"M:{type.FullName}.{method.Name}{args}";
    }

    // Doc-comment type names use the full name without generic arity noise for the
    // simple parameter types these controllers use (int, string, records, CancellationToken).
    private static string TypeName(Type type)
    {
        if (type.IsGenericType)
        {
            var def = type.GetGenericTypeDefinition().FullName!;
            def = def[..def.IndexOf('`')];
            var args = string.Join(",", type.GetGenericArguments().Select(TypeName));
            return $"{def}{{{args}}}";
        }
        return (type.FullName ?? type.Name).Replace('+', '.');
    }

    private static Dictionary<string, string> LoadSummaries()
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        var xmlPath = Path.ChangeExtension(Assembly.GetExecutingAssembly().Location, ".xml");
        if (!File.Exists(xmlPath))
            return result;

        foreach (var member in XDocument.Load(xmlPath).Descendants("member"))
        {
            var name = member.Attribute("name")?.Value;
            var summary = member.Element("summary")?.Value;
            if (name is not null && summary is not null)
                result[name] = summary.Trim();
        }
        return result;
    }
}

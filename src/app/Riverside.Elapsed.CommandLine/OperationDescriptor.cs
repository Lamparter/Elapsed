using System.Reflection;

namespace Riverside.Elapsed.CommandLine;

internal sealed record OperationDescriptor(
	string GroupName,
	string CommandName,
	string OperationPath,
	string HttpOperationPath,
	string Description,
	string HttpMethod,
	IReadOnlyList<PropertyInfo> BuilderPath,
	MethodInfo OperationMethod,
	Type? RequestBodyType,
	Type? RequestConfigurationType,
	Type? QueryParametersType);

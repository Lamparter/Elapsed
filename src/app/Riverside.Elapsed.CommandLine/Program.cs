using System.CommandLine;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Serialization;
using Microsoft.Kiota.Http.HttpClientLibrary;
using Microsoft.Kiota.Serialization.Json;
using Riverside.Elapsed.Auth.Token;

namespace Riverside.Elapsed.CommandLine;

public class Program
{
	private sealed record ParameterOptionBinding(string Key, PropertyInfo Property, Option<string?> Option);
	private static readonly Lazy<Dictionary<string, string>> OperationDescriptions = new(LoadOperationDescriptions);

	private static readonly JsonSerializerOptions JsonOptions = new()
	{
		WriteIndented = true,
		PropertyNameCaseInsensitive = true,
	};

	private static readonly Option<string?> BaseUrlOption = new("--base-url")
	{
		Arity = ArgumentArity.ZeroOrOne,
		Description = "Lapse API base URL.",
	};

	private static readonly Option<string?> TokenOption = new("--token")
	{
		Arity = ArgumentArity.ZeroOrOne,
		Description = "Bearer token for authenticated endpoints.",
	};

	private static readonly Option<FileInfo?> TokenFileOption = new("--token-file")
	{
		Arity = ArgumentArity.ZeroOrOne,
		Description = "Path to a file containing a bearer token.",
	};

	private static readonly Option<string?> BodyJsonOption = new("--body-json")
	{
		Arity = ArgumentArity.ZeroOrOne,
		Description = "Inline JSON request body.",
	};

	private static readonly Option<FileInfo?> BodyFileOption = new("--body-file")
	{
		Arity = ArgumentArity.ZeroOrOne,
		Description = "Path to JSON file containing request body.",
	};

	private static readonly Option<string[]> QueryOption = new("--query")
	{
		Arity = ArgumentArity.ZeroOrMore,
		Description = "Query value as key=value. Repeat for multiple values.",
	};

	public static async Task<int> Main(string[] args)
	{
		var root = new RootCommand("Elapsed CLI for Lapse (Hack Club's timelapse API)");

		root.Add(BaseUrlOption);
		root.Add(TokenOption);
		root.Add(TokenFileOption);

		root.Add(BuildConfigCommand());
		root.Add(BuildListOperationsCommand());

		var operationTree = BuildOperationTree();
		foreach (var command in operationTree)
		{
			root.Add(command);
		}

		return await root.Parse(args).InvokeAsync();
	}

	private static Command BuildConfigCommand()
	{
		var config = new Command("config") { Description = "Manage persisted CLI configuration." };

		var setToken = new Command("set-token") { Description = "Persist a bearer token for future runs." };
		var tokenArg = new Argument<string>("token")
		{
			Description = "Bearer token value.",
		};
		setToken.Add(tokenArg);
		setToken.SetAction(parseResult =>
		{
			var token = parseResult.GetValue(tokenArg);
			var cfg = LoadConfig();
			cfg.Token = token;
			SaveConfig(cfg);
			Console.WriteLine("Token saved.");
			return 0;
		});

		var clearToken = new Command("clear-token") { Description = "Clear persisted bearer token." };
		clearToken.SetAction(_ =>
		{
			var cfg = LoadConfig();
			cfg.Token = null;
			SaveConfig(cfg);
			Console.WriteLine("Token cleared.");
			return 0;
		});

		var show = new Command("show") { Description = "Show current CLI configuration." };
		show.SetAction(_ =>
		{
			var cfg = LoadConfig();
			var model = new
			{
				baseUrl = cfg.BaseUrl,
				token = MaskToken(cfg.Token),
				configPath = GetConfigPath(),
			};

			Console.WriteLine(JsonSerializer.Serialize(model, JsonOptions));
			return 0;
		});

		var auth = new Command("auth") { Description = "Authenticate online to get a bearer token." };
		auth.SetAction(async _ =>
		{
			return await HandleAuthAsync();
		});

		config.Add(setToken);
		config.Add(clearToken);
		config.Add(show);
		config.Add(auth);
		return config;
	}

	private static async Task<int> HandleAuthAsync()
	{
		try
		{
			const string clientId = Constants.ClientId;
			const string redirectUri = "http://localhost:8765/auth/callback";
			const string scopes = "timelapse:read timelapse:write comment:write user:read user:write";
			
			var baseUrl = LoadConfig().BaseUrl ?? Constants.Endpoint;

			var (codeVerifier, codeChallenge) = GeneratePKCEChallenge();
			var state = GenerateRandomString(32);

			var authorizeUrl = $"{baseUrl}/auth/authorize" +
				$"?client_id={Uri.EscapeDataString(clientId)}" +
				$"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
				$"&response_type=code" +
				$"&scope={Uri.EscapeDataString(scopes)}" +
				$"&state={Uri.EscapeDataString(state)}" +
				$"&code_challenge={Uri.EscapeDataString(codeChallenge)}" +
				$"&code_challenge_method=S256";

			Console.WriteLine("Opening browser for authentication...");
			Console.WriteLine($"If the browser doesn't open, visit: {authorizeUrl}");

			OpenBrowser(authorizeUrl);

			using var httpListener = new HttpListener();
			httpListener.Prefixes.Add("http://localhost:8765/");
			httpListener.Start();

			string? authCode = null;
			string? returnedState = null;
			
			try
			{
				var context = await httpListener.GetContextAsync();
				var request = context.Request;
				var response = context.Response;

				authCode = request.QueryString["code"];
				returnedState = request.QueryString["state"];
				var error = request.QueryString["error"];

				if (!string.IsNullOrEmpty(error))
				{
					response.StatusCode = 400;
					var errorMessage = $"Authentication error: {error}";
					var buffer = Encoding.UTF8.GetBytes(errorMessage);
					response.OutputStream.Write(buffer, 0, buffer.Length);
					response.Close();
					Console.Error.WriteLine(errorMessage);
					return 1;
				}

				if (string.IsNullOrEmpty(authCode) || returnedState != state)
				{
					response.StatusCode = 400;
					var errorMsg = "Invalid response from authentication server";
					var buffer = Encoding.UTF8.GetBytes(errorMsg);
					response.OutputStream.Write(buffer, 0, buffer.Length);
					response.Close();
					Console.Error.WriteLine(errorMsg);
					return 1;
				}

				response.StatusCode = 200;
				var successMessage = "Authentication successful! You can close this window.";
				var successBuffer = Encoding.UTF8.GetBytes(successMessage);
				response.OutputStream.Write(successBuffer, 0, successBuffer.Length);
				response.Close();
			}
			finally
			{
				httpListener.Stop();
			}

			Console.WriteLine("Exchanging authorisation code for token...");

			var adapter = new HttpClientRequestAdapter(new NoAuthProvider());
			adapter.BaseUrl = baseUrl;
			var client = new ApiClient(adapter);

			var tokenRequest = new TokenPostRequestBody
			{
				ClientId = clientId,
				Code = authCode,
				CodeVerifier = codeVerifier,
				RedirectUri = redirectUri,
				GrantType = new UntypedString("authorization_code")
			};

			var tokenResponse = await client.Auth.Token.PostAsTokenPostResponseAsync(tokenRequest);
			
			if (string.IsNullOrEmpty(tokenResponse?.AccessToken))
			{
				Console.Error.WriteLine("Failed to obtain access token from server.");
				return 1;
			}

			var cfg = LoadConfig();
			cfg.Token = tokenResponse.AccessToken;
			SaveConfig(cfg);

			Console.WriteLine("Authentication successful! Token has been saved.");
			Console.WriteLine($"Token: {MaskToken(tokenResponse.AccessToken)}");
			if (!string.IsNullOrEmpty(tokenResponse.RefreshToken))
			{
				Console.WriteLine($"Refresh token: {MaskToken(tokenResponse.RefreshToken)}");
			}

			return 0;
		}
		catch (Exception ex)
		{
			Console.Error.WriteLine($"Authentication failed: {ex.Message}");
			return 1;
		}
	}

	private static Command BuildListOperationsCommand()
	{
		var list = new Command("list-operations") { Description = "List all generated API operations that are available." };
		list.SetAction(_ =>
		{
			foreach (var descriptor in GetOperationDescriptors().OrderBy(x => x.OperationPath))
			{
				Console.WriteLine($"{descriptor.OperationPath} [{descriptor.HttpMethod}]");
			}

			return 0;
		});
		return list;
	}

	private static IReadOnlyList<Command> BuildOperationTree()
	{
		var grouped = GetOperationDescriptors().GroupBy(x => x.GroupName).OrderBy(x => x.Key);
		var commands = new List<Command>();

		foreach (var group in grouped)
		{
			var groupCommand = new Command(group.Key) { Description = $"Operations under /{group.Key}" };
			foreach (var operation in group.OrderBy(x => x.CommandName))
			{
				var opCommand = new Command(operation.CommandName) { Description = operation.Description };
				var usedAliases = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
				{
					"--query",
					"--body-json",
					"--body-file",
				};

				var queryOptionBindings = BuildParameterOptionBindings(operation.QueryParametersType, isQueryParameter: true, usedAliases);
				var bodyOptionBindings = BuildParameterOptionBindings(operation.RequestBodyType, isQueryParameter: false, usedAliases);

				//opCommand.Add(QueryOption);
				opCommand.Add(BodyJsonOption);
				opCommand.Add(BodyFileOption);

				foreach (var binding in queryOptionBindings)
				{
					opCommand.Add(binding.Option);
				}

				foreach (var binding in bodyOptionBindings)
				{
					opCommand.Add(binding.Option);
				}

				opCommand.SetAction(async (parseResult, cancellationToken) =>
				{
					return await ExecuteOperationAsync(operation, parseResult, cancellationToken, queryOptionBindings, bodyOptionBindings);
				});

				groupCommand.Add(opCommand);
			}

			commands.Add(groupCommand);
		}

		return commands;
	}

	private static async Task<int> ExecuteOperationAsync(
		OperationDescriptor operation,
		ParseResult parseResult,
		CancellationToken cancellationToken,
		IReadOnlyList<ParameterOptionBinding> queryOptionBindings,
		IReadOnlyList<ParameterOptionBinding> bodyOptionBindings)
	{
		try
		{
			var config = LoadConfig();
			var baseUrl = parseResult.GetValue(BaseUrlOption)
				?? config.BaseUrl
				?? Constants.Endpoint;

			var token = ResolveToken(parseResult, config);
			if (string.IsNullOrWhiteSpace(token))
			{
				Console.Error.WriteLine($"Warning: no authentication token configured. Executing '{operation.OperationPath}' unauthenticated.");
				Console.Error.WriteLine("Run 'config auth' to authenticate.");
			}

			var adapter = new HttpClientRequestAdapter(new StaticBearerAuthProvider(token));
			adapter.BaseUrl = baseUrl;
			var client = new ApiClient(adapter);

			var builder = ResolveBuilder(client, operation.BuilderPath);

			var bodyJson = ResolveBodyJson(parseResult);
			var queryMap = ParseQueryValues(parseResult.GetValue(QueryOption) ?? []);
			ApplyNamedQueryOptionValues(queryMap, queryOptionBindings, parseResult);
			var bodyArg = BuildBodyArgument(operation.RequestBodyType, bodyJson, bodyOptionBindings, parseResult);
			var requestConfigArg = BuildRequestConfigurationArgument(operation.RequestConfigurationType, operation.QueryParametersType, queryMap);

			var args = BuildMethodArgs(operation.OperationMethod, bodyArg, requestConfigArg, cancellationToken);
			var task = (Task?)operation.OperationMethod.Invoke(builder, args);

			if (task is null)
			{
				Console.Error.WriteLine("Failed to execute operation: no task returned.");
				return 1;
			}

			await task.ConfigureAwait(false);

			var result = GetTaskResult(task);
			if (result is null)
			{
				Console.WriteLine("{\"status\":\"ok\"}");
				return 0;
			}

			WriteObjectAsJson(result);
			return 0;
		}
		catch (Exception ex)
		{
			Console.Error.WriteLine($"Operation failed: {ex.Message}");
			return 1;
		}
	}

	private static IReadOnlyList<ParameterOptionBinding> BuildParameterOptionBindings(Type? modelType, bool isQueryParameter, HashSet<string> usedAliases)
	{
		if (modelType is null)
		{
			return [];
		}

		var bindings = new List<ParameterOptionBinding>();
		var properties = modelType
			.GetProperties(BindingFlags.Public | BindingFlags.Instance)
			.Where(p => p.CanWrite && p.SetMethod?.IsPublic == true)
			.Where(p => p.Name != "AdditionalData")
			.OrderBy(p => p.Name);

		foreach (var property in properties)
		{
			var key = isQueryParameter ? GetQueryKey(property) : property.Name;
			var preferredName = ToOptionName(key);
			if (string.IsNullOrWhiteSpace(preferredName))
			{
				preferredName = ToOptionName(property.Name);
			}

			var alias = $"--{preferredName}";
			if (!usedAliases.Add(alias))
			{
				alias = isQueryParameter
					? $"--query-{preferredName}"
					: $"--body-{preferredName}";

				if (!usedAliases.Add(alias))
				{
					continue;
				}
			}

			var option = new Option<string?>(alias)
			{
				Arity = ArgumentArity.ZeroOrOne,
				Description = isQueryParameter
					? $"Query parameter '{key}'."
					: $"Request body field '{property.Name}'.",
			};

			bindings.Add(new ParameterOptionBinding(key, property, option));
		}

		return bindings;
	}

	private static string ToOptionName(string value)
	{
		var decoded = Uri.UnescapeDataString(value).Trim();
		decoded = decoded.TrimStart('$', '@');
		var cleaned = new string(decoded
			.Select(c => char.IsLetterOrDigit(c) || c == '-' || c == '_' ? c : '-')
			.ToArray())
			.Trim('-');

		if (string.IsNullOrWhiteSpace(cleaned))
		{
			return string.Empty;
		}

		cleaned = cleaned.Replace('_', '-');
		return ToKebabCase(cleaned);
	}

	private static void ApplyNamedQueryOptionValues(
		IDictionary<string, string> queryMap,
		IReadOnlyList<ParameterOptionBinding> queryOptionBindings,
		ParseResult parseResult)
	{
		foreach (var binding in queryOptionBindings)
		{
			var value = parseResult.GetValue(binding.Option);
			if (value is null)
			{
				continue;
			}

			queryMap[binding.Key] = value;
		}
	}

	private static object? ResolveBuilder(ApiClient client, IReadOnlyList<PropertyInfo> builderPath)
	{
		object current = client;
		foreach (var property in builderPath)
		{
			current = property.GetValue(current) ?? throw new InvalidOperationException($"Could not resolve property {property.Name}.");
		}

		return current;
	}

	private static string? ResolveToken(ParseResult parseResult, CliConfig config)
	{
		var optionToken = parseResult.GetValue(TokenOption);
		if (!string.IsNullOrWhiteSpace(optionToken))
		{
			return optionToken;
		}

		var tokenFile = parseResult.GetValue(TokenFileOption);
		if (tokenFile is not null)
		{
			if (!tokenFile.Exists)
			{
				throw new FileNotFoundException("Token file does not exist.", tokenFile.FullName);
			}

			return File.ReadAllText(tokenFile.FullName).Trim();
		}

		return config.Token;
	}

	private static string? ResolveBodyJson(ParseResult parseResult)
	{
		var bodyJson = parseResult.GetValue(BodyJsonOption);
		var bodyFile = parseResult.GetValue(BodyFileOption);

		if (!string.IsNullOrWhiteSpace(bodyJson) && bodyFile is not null)
		{
			throw new InvalidOperationException("Use either --body-json or --body-file, not both.");
		}

		if (bodyFile is not null)
		{
			if (!bodyFile.Exists)
			{
				throw new FileNotFoundException("Body file does not exist.", bodyFile.FullName);
			}

			return File.ReadAllText(bodyFile.FullName);
		}

		return bodyJson;
	}

	private static object? BuildBodyArgument(
		Type? bodyType,
		string? bodyJson,
		IReadOnlyList<ParameterOptionBinding> bodyOptionBindings,
		ParseResult parseResult)
	{
		var hasBodyOptions = false;
		foreach (var binding in bodyOptionBindings)
		{
			if (parseResult.GetValue(binding.Option) is not null)
			{
				hasBodyOptions = true;
				break;
			}
		}

		if (bodyType is null)
		{
			if (!string.IsNullOrWhiteSpace(bodyJson))
			{
				throw new InvalidOperationException("This endpoint does not accept a request body.");
			}

			if (hasBodyOptions)
			{
				throw new InvalidOperationException("This endpoint does not accept request body fields.");
			}

			return null;
		}

		if (!string.IsNullOrWhiteSpace(bodyJson) && hasBodyOptions)
		{
			throw new InvalidOperationException("Use either --body-json/--body-file or body field options, not both.");
		}

		if (hasBodyOptions)
		{
			var bodyObject = Activator.CreateInstance(bodyType)
				?? throw new InvalidOperationException($"Could not construct request body type {bodyType.FullName}.");

			foreach (var binding in bodyOptionBindings)
			{
				var value = parseResult.GetValue(binding.Option);
				if (value is null)
				{
					continue;
				}

				var converted = ConvertString(value, binding.Property.PropertyType);
				binding.Property.SetValue(bodyObject, converted);
			}

			return bodyObject;
		}

		if (string.IsNullOrWhiteSpace(bodyJson))
		{
			throw new InvalidOperationException("This endpoint requires a JSON body (--body-json or --body-file).");
		}

		return DeserializeParsable(bodyType, bodyJson);
	}

	private static object? BuildRequestConfigurationArgument(Type? requestConfigType, Type? queryType, IReadOnlyDictionary<string, string> queryValues)
	{
		if (requestConfigType is null)
		{
			if (queryValues.Count > 0)
			{
				throw new InvalidOperationException("This endpoint does not support query parameters.");
			}

			return null;
		}

		if (queryType is null)
		{
			return null;
		}

		var helper = typeof(Program)
			.GetMethod(nameof(CreateRequestConfiguration), BindingFlags.Static | BindingFlags.NonPublic)
			?.MakeGenericMethod(queryType)
			?? throw new InvalidOperationException("Could not create request configuration helper.");

		return helper.Invoke(null, [queryValues]);
	}

	private static Action<RequestConfiguration<TQuery>> CreateRequestConfiguration<TQuery>(IReadOnlyDictionary<string, string> queryValues)
		where TQuery : class, new()
	{
		return config =>
		{
			var query = new TQuery();
			PopulateQueryObject(query, queryValues);
			config.QueryParameters = query;
		};
	}

	private static void PopulateQueryObject<TQuery>(TQuery queryObject, IReadOnlyDictionary<string, string> queryValues)
		where TQuery : class
	{
		var properties = typeof(TQuery).GetProperties(BindingFlags.Public | BindingFlags.Instance);
		var keyMap = properties.ToDictionary(GetQueryKey, StringComparer.OrdinalIgnoreCase);

		foreach (var (key, value) in queryValues)
		{
			if (!keyMap.TryGetValue(key, out var property))
			{
				throw new InvalidOperationException($"Unknown query parameter '{key}'.");
			}

			var converted = ConvertString(value, property.PropertyType);
			property.SetValue(queryObject, converted);
		}
	}

	private static string GetQueryKey(PropertyInfo property)
	{
		var queryAttribute = property.GetCustomAttributes()
			.FirstOrDefault(x => x.GetType().Name == "QueryParameterAttribute");
		if (queryAttribute is null)
		{
			return property.Name;
		}

		var nameProperty = queryAttribute.GetType().GetProperty("TemplateName");
		var explicitName = nameProperty?.GetValue(queryAttribute) as string;
		return string.IsNullOrWhiteSpace(explicitName) ? property.Name : explicitName;
	}

	private static object? ConvertString(string value, Type destinationType)
	{
		var nullableType = Nullable.GetUnderlyingType(destinationType);
		if (nullableType is not null)
		{
			if (string.IsNullOrWhiteSpace(value))
			{
				return null;
			}

			return ConvertString(value, nullableType);
		}

		if (destinationType == typeof(string))
		{
			return value;
		}

		if (destinationType == typeof(Guid))
		{
			return Guid.Parse(value);
		}

		if (destinationType.IsEnum)
		{
			return Enum.Parse(destinationType, value, ignoreCase: true);
		}

		return JsonSerializer.Deserialize(value, destinationType, JsonOptions);
	}

	private static object DeserializeParsable(Type targetType, string json)
	{
		var createMethod = targetType.GetMethod(
			"CreateFromDiscriminatorValue",
			BindingFlags.Public | BindingFlags.Static,
			binder: null,
			[typeof(IParseNode)],
			modifiers: null);

		if (createMethod is null)
		{
			throw new InvalidOperationException($"Type {targetType.FullName} is not a Kiota parsable type.");
		}

		var delegateType = typeof(ParsableFactory<>).MakeGenericType(targetType);
		var parseFactory = createMethod.CreateDelegate(delegateType);

		var parseNodeFactory = new JsonParseNodeFactory();
		using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
		var rootNode = parseNodeFactory
			.GetRootParseNodeAsync("application/json", stream)
			.GetAwaiter()
			.GetResult();

		var getObjectValue = rootNode.GetType()
			.GetMethods(BindingFlags.Public | BindingFlags.Instance)
			.First(m => m.Name == "GetObjectValue" && m.IsGenericMethodDefinition)
			.MakeGenericMethod(targetType);

		return getObjectValue.Invoke(rootNode, [parseFactory])
			?? throw new InvalidOperationException($"Failed to deserialise JSON into {targetType.FullName}.");
	}

	private static object?[] BuildMethodArgs(MethodInfo method, object? bodyArg, object? requestConfigArg, CancellationToken cancellationToken)
	{
		var parameters = method.GetParameters();
		var args = new object?[parameters.Length];

		var bodyAssigned = false;
		var configAssigned = false;

		for (var i = 0; i < parameters.Length; i++)
		{
			var parameter = parameters[i];
			var parameterType = parameter.ParameterType;

			if (parameterType == typeof(CancellationToken))
			{
				args[i] = cancellationToken;
				continue;
			}

			if (IsRequestConfigurationParameter(parameterType))
			{
				args[i] = requestConfigArg;
				configAssigned = true;
				continue;
			}

			if (!bodyAssigned)
			{
				args[i] = bodyArg;
				bodyAssigned = true;
				continue;
			}

			args[i] = parameter.HasDefaultValue ? parameter.DefaultValue : null;
		}

		if (bodyArg is not null && !bodyAssigned)
		{
			throw new InvalidOperationException("Could not map request body to operation method parameters.");
		}

		if (requestConfigArg is not null && !configAssigned)
		{
			throw new InvalidOperationException("Could not map query configuration to operation method parameters.");
		}

		return args;
	}

	private static object? GetTaskResult(Task task)
	{
		var type = task.GetType();
		if (!type.IsGenericType)
		{
			return null;
		}

		var resultProperty = type.GetProperty("Result", BindingFlags.Public | BindingFlags.Instance);
		return resultProperty?.GetValue(task);
	}

	private static void WriteObjectAsJson(object value)
	{
		try
		{
			var json = JsonSerializer.Serialize(value, value.GetType(), JsonOptions);
			Console.WriteLine(json);
		}
		catch
		{
			Console.WriteLine(value.ToString());
		}
	}

	private static Dictionary<string, string> ParseQueryValues(IEnumerable<string> queryPairs)
	{
		var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		foreach (var pair in queryPairs)
		{
			var index = pair.IndexOf('=');
			if (index <= 0)
			{
				throw new InvalidOperationException($"Invalid --query value '{pair}'. Expected key=value.");
			}

			var key = pair[..index].Trim();
			var value = pair[(index + 1)..].Trim();

			if (string.IsNullOrWhiteSpace(key))
			{
				throw new InvalidOperationException($"Invalid query key in '{pair}'.");
			}

			result[key] = value;
		}

		return result;
	}

	private static List<OperationDescriptor> GetOperationDescriptors()
	{
		var rootType = typeof(ApiClient);
		var topLevelGroups = rootType
			.GetProperties(BindingFlags.Public | BindingFlags.Instance)
			.Where(p => IsRequestBuilderType(p.PropertyType))
			.OrderBy(p => p.Name)
			.ToArray();

		var results = new List<OperationDescriptor>();
		foreach (var group in topLevelGroups)
		{
			var groupName = ToKebabCase(NormalizeKeywordPropertyName(group.Name));
			var endpointProperties = group.PropertyType
				.GetProperties(BindingFlags.Public | BindingFlags.Instance)
				.Where(p => IsRequestBuilderType(p.PropertyType))
				.OrderBy(p => p.Name)
				.ToArray();

			foreach (var endpoint in endpointProperties)
			{
				var operationMethod = FindPrimaryOperationMethod(endpoint.PropertyType);
				if (operationMethod is null)
				{
					continue;
				}

				var requestBodyType = FindRequestBodyType(operationMethod);
				var requestConfigType = FindRequestConfigurationType(operationMethod);
				var queryType = FindQueryType(requestConfigType);
				var operationName = ToKebabCase(NormalizeKeywordPropertyName(endpoint.Name));
				var httpMethod = operationMethod.Name.StartsWith("Get", StringComparison.Ordinal) ? "GET"
					: operationMethod.Name.StartsWith("Post", StringComparison.Ordinal) ? "POST"
					: operationMethod.Name.StartsWith("Patch", StringComparison.Ordinal) ? "PATCH"
					: operationMethod.Name.StartsWith("Delete", StringComparison.Ordinal) ? "DELETE"
					: "UNKNOWN";

				var descriptor = new OperationDescriptor(
					GroupName: groupName,
					CommandName: operationName,
					OperationPath: $"{groupName}/{operationName}",
                    Description: GetOperationDescription($"{groupName}/{operationName}", httpMethod, operationMethod)
						?? $"{httpMethod} {groupName}/{operationName}",
					HttpMethod: httpMethod,
					BuilderPath: [group, endpoint],
					OperationMethod: operationMethod,
					RequestBodyType: requestBodyType,
					RequestConfigurationType: requestConfigType,
					QueryParametersType: queryType);

				results.Add(descriptor);
			}
		}

		return results;
	}

	private static string? GetOperationDescription(string operationPath, string httpMethod, MethodInfo method)
	{
		var key = $"{httpMethod}:{operationPath}";
		if (OperationDescriptions.Value.TryGetValue(key, out var description) && !string.IsNullOrWhiteSpace(description))
		{
			return description;
		}

		return GetSummary(method);
	}

	private static Dictionary<string, string> LoadOperationDescriptions()
	{
		var descriptions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		try
		{
			var documentPath = FindOpenApiDocumentPath();
			if (string.IsNullOrWhiteSpace(documentPath) || !File.Exists(documentPath))
			{
				return descriptions;
			}

			using var stream = File.OpenRead(documentPath);
			using var document = JsonDocument.Parse(stream);
			if (!document.RootElement.TryGetProperty("paths", out var pathsElement))
			{
				return descriptions;
			}

			foreach (var pathProperty in pathsElement.EnumerateObject())
			{
				var operationPath = pathProperty.Name.TrimStart('/');
				if (pathProperty.Value.ValueKind != JsonValueKind.Object)
				{
					continue;
				}

				foreach (var methodProperty in pathProperty.Value.EnumerateObject())
				{
					if (methodProperty.Value.ValueKind != JsonValueKind.Object)
					{
						continue;
					}

					var method = methodProperty.Name.ToUpperInvariant();
					var description = GetDescriptionValue(methodProperty.Value);
					if (string.IsNullOrWhiteSpace(description))
					{
						continue;
					}

					descriptions[$"{method}:{operationPath}"] = description;
				}
			}
		}
		catch
		{
		}

		return descriptions;
	}

	private static string? FindOpenApiDocumentPath()
	{
		var outputPath = Path.Combine(AppContext.BaseDirectory, "Riverside.Elapsed.json");
		if (File.Exists(outputPath))
		{
			return outputPath;
		}

		var current = new DirectoryInfo(AppContext.BaseDirectory);
		for (var i = 0; i < 8 && current is not null; i++)
		{
			var candidate = Path.Combine(current.FullName, "src", "core", "Riverside.Elapsed", "Riverside.Elapsed.json");
			if (File.Exists(candidate))
			{
				return candidate;
			}

			current = current.Parent;
		}

		return null;
	}

	private static string? GetDescriptionValue(JsonElement operationElement)
	{
		if (operationElement.TryGetProperty("description", out var descriptionElement)
			&& descriptionElement.ValueKind == JsonValueKind.String)
		{
			return descriptionElement.GetString()?.Trim();
		}

		if (operationElement.TryGetProperty("summary", out var summaryElement)
			&& summaryElement.ValueKind == JsonValueKind.String)
		{
			return summaryElement.GetString()?.Trim();
		}

		return null;
	}

	private static string? GetSummary(MethodInfo method)
	{
		var summary = method
			.DeclaringType?
			.GetCustomAttributesData()
			.FirstOrDefault(a => a.AttributeType.Name == "DescriptionAttribute")
			?.ConstructorArguments
			.FirstOrDefault()
			.Value as string;
		return summary;
	}

	private static bool IsRequestBuilderType(Type type)
		=> typeof(BaseRequestBuilder).IsAssignableFrom(type);

	private static MethodInfo? FindPrimaryOperationMethod(Type requestBuilderType)
	{
		return requestBuilderType
			.GetMethods(BindingFlags.Public | BindingFlags.Instance)
			.Where(m => m.Name.EndsWith("Async", StringComparison.Ordinal))
			.Where(m =>
				m.Name.StartsWith("GetAs", StringComparison.Ordinal)
				|| m.Name.StartsWith("PostAs", StringComparison.Ordinal)
				|| m.Name.StartsWith("PatchAs", StringComparison.Ordinal)
				|| m.Name.StartsWith("DeleteAs", StringComparison.Ordinal))
			.Where(m => m.GetCustomAttributes(typeof(ObsoleteAttribute), false).Length == 0)
			.OrderBy(m => m.Name)
			.FirstOrDefault();
	}

	private static Type? FindRequestBodyType(MethodInfo operationMethod)
	{
		return operationMethod.GetParameters()
			.Select(p => p.ParameterType)
			.FirstOrDefault(t =>
				t != typeof(CancellationToken)
				&& !IsRequestConfigurationParameter(t));
	}

	private static Type? FindRequestConfigurationType(MethodInfo operationMethod)
	{
		return operationMethod.GetParameters()
			.Select(p => p.ParameterType)
			.FirstOrDefault(IsRequestConfigurationParameter);
	}

	private static Type? FindQueryType(Type? requestConfigurationType)
	{
		if (requestConfigurationType is null || !requestConfigurationType.IsGenericType)
		{
			return null;
		}

		var genericDefinition = requestConfigurationType.GetGenericTypeDefinition();
		if (genericDefinition != typeof(Action<>))
		{
			return null;
		}

		var actionArg = requestConfigurationType.GetGenericArguments()[0];
		if (!actionArg.IsGenericType)
		{
			return null;
		}

		var requestConfigGenericDef = actionArg.GetGenericTypeDefinition();
		if (requestConfigGenericDef != typeof(RequestConfiguration<>))
		{
			return null;
		}

		return actionArg.GetGenericArguments()[0];
	}

	private static bool IsRequestConfigurationParameter(Type type)
	{
		if (!type.IsGenericType)
		{
			return false;
		}

		if (type.GetGenericTypeDefinition() != typeof(Action<>))
		{
			return false;
		}

		var arg = type.GetGenericArguments()[0];
		return arg.IsGenericType && arg.GetGenericTypeDefinition() == typeof(RequestConfiguration<>);
	}

	private static string NormalizeKeywordPropertyName(string name)
	{
		return name.EndsWith("Path", StringComparison.Ordinal) ? name[..^4] : name;
	}

	private static string ToKebabCase(string value)
	{
		var sb = new StringBuilder(value.Length + 8);
		for (var i = 0; i < value.Length; i++)
		{
			var c = value[i];
			if (char.IsUpper(c))
			{
				if (i > 0)
				{
					sb.Append('-');
				}

				sb.Append(char.ToLowerInvariant(c));
			}
			else
			{
				sb.Append(c);
			}
		}

		return sb.ToString();
	}

	private static string GetConfigPath()
	{
		var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
		var folder = Path.Combine(appData, "Riverside", "Elapsed");
		Directory.CreateDirectory(folder);
		return Path.Combine(folder, "config.json");
	}

	private static CliConfig LoadConfig()
	{
		var path = GetConfigPath();
		if (!File.Exists(path))
		{
			return new CliConfig();
		}

		try
		{
			var json = File.ReadAllText(path);
			return JsonSerializer.Deserialize<CliConfig>(json, JsonOptions) ?? new CliConfig();
		}
		catch
		{
			return new CliConfig();
		}
	}

	private static void SaveConfig(CliConfig config)
	{
		var path = GetConfigPath();
		var json = JsonSerializer.Serialize(config, JsonOptions);
		File.WriteAllText(path, json);
	}

	private static string? MaskToken(string? token)
	{
		if (string.IsNullOrWhiteSpace(token))
		{
			return null;
		}

		if (token.Length <= 8)
		{
			return "********";
		}

		return $"{token[..4]}...{token[^4..]}";
	}

	private static (string verifier, string challenge) GeneratePKCEChallenge()
	{
		var verifier = GenerateRandomString(128);

		using var sha256 = System.Security.Cryptography.SHA256.Create();
		var challengeBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(verifier));
		var challenge = Convert.ToBase64String(challengeBytes)
			.Replace("+", "-")
			.Replace("/", "_")
			.TrimEnd('=');

		return (verifier, challenge);
	}

	private static string GenerateRandomString(int length)
	{
		const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
		var random = new Random();
		return new string(Enumerable.Range(0, length)
			.Select(_ => chars[random.Next(chars.Length)])
			.ToArray());
	}

	private static void OpenBrowser(string url)
	{
		try
		{
			if (OperatingSystem.IsWindows())
			{
				System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
				{
					FileName = url,
					UseShellExecute = true,
				});
			}
			else if (OperatingSystem.IsMacOS())
			{
				System.Diagnostics.Process.Start("open", url);
			}
			else if (OperatingSystem.IsLinux())
			{
				System.Diagnostics.Process.Start("xdg-open", url);
			}
		}
		catch
		{
			// the user can still manually visit the URL
		}
	}
}

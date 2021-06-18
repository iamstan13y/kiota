using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using Kiota.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Kiota {
    public static class KiotaHost {
        public static RootCommand GetRootCommand()
        {
            var configuration = LoadDefaultConfiguration();
            var outputOption = new Option("--output", "The output directory path for the generated code files.") { Argument = new Argument<string>(() => "./output") };
            outputOption.AddAlias("-o");
            
            var languageOption = new Option("--language", "The target language for the generated code files.") { Argument = new Argument<GenerationLanguage?>(() => GenerationLanguage.CSharp) };
            languageOption.AddAlias("-l");
            AddEnumValidator<GenerationLanguage>(languageOption.Argument, "language");
            var classOption = new Option("--class-name", "The class name to use for the core client class.") { Argument = new Argument<string>(() => "ApiClient") };
            classOption.AddAlias("-c");
            AddStringRegexValidator(classOption.Argument, @"^[a-zA-Z_][\w_-]+", "class name");

            var namespaceOption = new Option("--namespace-name", "The namespace to use for the core client class specified with the --class-name option.") { Argument = new Argument<string>(() => "ApiClient") };
            namespaceOption.AddAlias("-n");
            AddStringRegexValidator(namespaceOption.Argument, @"^[\w][\w\._-]+", "namespace name");

            var logLevelOption = new Option("--loglevel", "The log level to use when logging messages to the main output.") { Argument = new Argument<LogLevel>(() => LogLevel.Warning)};
            logLevelOption.AddAlias("--ll");
            AddEnumValidator<LogLevel>(logLevelOption.Argument, "log level");
            var descriptionOption = new Option("--openapi", "The path to the OpenAPI description file used to generate the code files.") {Argument = new Argument<string>(() => "openapi.yml")};
            descriptionOption.AddAlias("-d");

            var backingStoreOption = new Option("--backing-store", "The fully qualified name for the backing store class to use.") {Argument = new Argument<string>()};
            backingStoreOption.AddAlias("-b");

            var command = new RootCommand {
                outputOption,
                languageOption,
                descriptionOption,
                backingStoreOption,
                classOption,
                logLevelOption,
                namespaceOption,
            };
            command.Handler = CommandHandler.Create<string, GenerationLanguage?, string, string, string, LogLevel, string>(async (output, language, openapi, backingstore, classname, loglevel, namespacename) =>
            {
                if (!string.IsNullOrEmpty(output))
                    configuration.OutputPath = output;
                if (!string.IsNullOrEmpty(openapi))
                    configuration.OpenAPIFilePath = openapi;
                if (!string.IsNullOrEmpty(classname))
                    configuration.ClientClassName = classname;
                if (!string.IsNullOrEmpty(namespacename))
                    configuration.ClientNamespaceName = namespacename;
                if (language.HasValue)
                    configuration.Language = language.Value;
                if(!string.IsNullOrEmpty(backingstore))
                    configuration.BackingStore = backingstore.Trim('\'', '"'); //npm modules can start with @ which prompts some terminals to read response files and quotes are not automatically trimmed by the framework

                #if DEBUG
                loglevel = loglevel > LogLevel.Debug ? LogLevel.Debug : loglevel;
                #endif

                configuration.OpenAPIFilePath = GetAbsolutePath(configuration.OpenAPIFilePath);
                configuration.OutputPath = GetAbsolutePath(configuration.OutputPath);

                var logger = LoggerFactory.Create((builder) => {
                    builder
                        .AddConsole()
#if DEBUG
                        .AddDebug()
#endif
                        .SetMinimumLevel(loglevel);
                }).CreateLogger<KiotaBuilder>();

                logger.LogTrace($"configuration: {JsonSerializer.Serialize(configuration)}");

                await new KiotaBuilder(logger, configuration).GenerateSDK();

            });
            return command;
        }
        private static void AddStringRegexValidator(Argument argument, string pattern, string parameterName) {
            var validator = new Regex(pattern);
            argument.AddValidator((input) => {
                if(input.Tokens.Any() &&
                    !validator.IsMatch(input.Tokens[0].Value))
                        return $"{input.Tokens[0].Value} is not a valid {parameterName} for the client, the {parameterName} must conform to {pattern}";
                return null;
            });
        }
        private static void AddEnumValidator<T>(Argument argument, string parameterName) where T: struct, Enum {
            argument.AddValidator((input) => {
                if(input.Tokens.Any() &&
                    !Enum.TryParse<T>(input.Tokens[0].Value, true, out var _)) {
                        var validOptionsList = Enum.GetValues<T>().Select(x => x.ToString()).Aggregate((x, y) => x + ", " + y);
                        return $"{input.Tokens[0].Value} is not a supported generation {parameterName}, supported values are {validOptionsList}";
                    }
                return null;
            });
        }
        private static GenerationConfiguration LoadDefaultConfiguration() {
            var builder = new ConfigurationBuilder();
            var configuration = builder.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                    .AddEnvironmentVariables(prefix: "KIOTA_")
                    .Build();
            var configObject = new GenerationConfiguration();
            configuration.Bind(configObject);
            return configObject;
        }

        private static string GetAbsolutePath(string source) => Path.IsPathRooted(source) || source.StartsWith("http") ? source : Path.Combine(Directory.GetCurrentDirectory(), source);

    }
    
}

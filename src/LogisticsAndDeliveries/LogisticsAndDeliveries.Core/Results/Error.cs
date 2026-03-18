using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace LogisticsAndDeliveries.Core.Results
{
    public record Error
    {
        public string Code
        {
            get;
        }
        public string Description
        {
            get;
        }
        public ErrorType Type
        {
            get;
        }
        public string StructuredMessage
        {
            get;
        }

        public static readonly Error None = new(string.Empty, string.Empty, ErrorType.Failure);
        public static readonly Error NullValue = new(
            "General.Null",
            "Null value was provided",
            ErrorType.Failure);

        public Error(string code, string structuredMessage, ErrorType type, params string[]? args)
        {
            if (structuredMessage == null)
            {
                structuredMessage = string.Empty;
            }
            StructuredMessage = structuredMessage;
            Description = BuildMessage(StructuredMessage, args);
            Code = code;
            Type = type;
        }

        private string BuildMessage(string structuredMessage, params object[]? args)
        {
            if (args == null || args.Length == 0)
            {
                return structuredMessage;
            }
            var placeholders = Regex.Matches(structuredMessage, @"\{(\w+)\}");
            StringBuilder result = new(structuredMessage);
            int index = 0;
            foreach (Match match in placeholders)
            {
                if (index >= args.Length)
                    break; // Evita IndexOutOfRange si hay menos valores que placeholders

                var placeholder = match.Value; // Ej: {nombre}
                var valor = args[index]?.ToString() ?? string.Empty;

                result = result.Replace(placeholder, valor);
                index++;
            }
            return result.ToString();
        }

        public static Error Failure(string code, string structuredMessage, params string[]? args) =>
            new(code, structuredMessage, ErrorType.Failure, args);

        public static Error NotFound(string code, string structuredMessage, params string[]? args) =>
            new(code, structuredMessage, ErrorType.NotFound, args);

        public static Error Problem(string code, string structuredMessage, params string[]? args) =>
            new(code, structuredMessage, ErrorType.Problem, args);

        public static Error Conflict(string code, string structuredMessage, params string[]? args) =>
            new(code, structuredMessage, ErrorType.Conflict, args);
    }
}
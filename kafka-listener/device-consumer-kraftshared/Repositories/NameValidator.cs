using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Dapper;

namespace Device.Consumer.KraftShared.Repositories
{
    public class NameValidator
    {
        private const string SUFFIX = "copy";
        private readonly IDictionary<string, List<int>> _duplicateNames = new Dictionary<string, List<int>>();
        private readonly string _tableName;
        private readonly string _columnName;
        private readonly string _conditions;
        private string _seperator;
        public char Seperator
        {
            set
            {
                _seperator = value != '\0' ? $"{value}" : string.Empty;
            }
        }
        public AbstractCondition AdditionalConditions { get; set; }

        public NameValidator(string tableName, string columnName, AbstractCondition condition = null)
        {
            _tableName = tableName;
            _columnName = columnName;
            _conditions = condition?.ToString(true);
        }

        public static string GetCopyPattern(string name, string seperator)
        {
            return $@"{EscapePattern(name)}\{seperator}{SUFFIX}%";
        }

        public static string AppendCopy(string name, string seperator, ICollection<int> duplicate, int offset = 0)
        {
            int count = offset;
            var result = name;
            while (duplicate.Contains(count))
            {
                result = string.Concat(result, seperator, SUFFIX);
                count++;
            }
            duplicate.Add(count);
            return result;
        }

        public static string EscapePattern(string input)
        {
            return Regex.Replace(input, @$"[%_\\]", match => string.Format(@"\{0}", match.Value));
        }

        public static string EscapeRegex(string input)
        {
            return Regex.Replace(input, @$"[.$^{{[(|)*+?\\]", match => string.Format(@"\{0}", match.Value));
        }

        public async Task<string> ValidateDuplicateNameAsync(string name, IDbConnection connection, string key = "", bool ignoreCase = true)
        {
            var option = ignoreCase ? RegexOptions.IgnoreCase : RegexOptions.None;
            var trailingSuffixRegex = new Regex($"({EscapeRegex(_seperator)}{SUFFIX})+$", option);

            // remove existing suffix from input
            var nonSuffix = trailingSuffixRegex.Replace(name, string.Empty);
            key = string.Concat(key, ignoreCase ? nonSuffix.ToLowerInvariant() : nonSuffix);

            if (!_duplicateNames.TryGetValue(key, out var duplicate))
            {
                var duplicateCandidates = await GetDuplicateCandidatesAsync(nonSuffix, connection);

                // match only candidates that does not have other substring between non suffix 'name' and 'suffix'
                var str = $"^{EscapeRegex(nonSuffix)}({EscapeRegex(_seperator)}{SUFFIX})*$";
                var regex = new Regex(str, option);
                _duplicateNames[key] = duplicate = new List<int>();
                foreach (var duplicateName in duplicateCandidates.Where(name => regex.IsMatch(name)))
                {
                    // extract, count and register number repeated suffix
                    var trailingSuffix = trailingSuffixRegex.Match(duplicateName).Value;
                    duplicate.Add(Regex.Matches(trailingSuffix, SUFFIX, option).Count);
                }
            }

            // count current name repeated suffix to use as start count for append copy
            var offset = Regex.Matches(trailingSuffixRegex.Match(name).Value, SUFFIX, RegexOptions.IgnoreCase).Count;
            return AppendCopy(name, _seperator, duplicate, offset);
        }

        private Task<IEnumerable<string>> GetDuplicateCandidatesAsync(string name, IDbConnection connection)
        {
            var query = $@"SELECT {_columnName} FROM {_tableName} WHERE ({_columnName} ILIKE @Name OR {_columnName} ILIKE @Pattern ESCAPE '\'){_conditions}";
            if (AdditionalConditions?.IsValid ?? false)
            {
                query = string.Concat(query, AdditionalConditions.ToString(true));
            }
            return connection.QueryAsync<string>(query, new
            {
                Name = EscapePattern(name),
                Pattern = GetCopyPattern(name, _seperator)
            });
        }
    }

    public enum LogicalOperation
    {
        AND,
        OR
    }
    public abstract class AbstractCondition
    {
        protected LogicalOperation _logical;
        public abstract bool IsValid { get; }

        public AbstractCondition(LogicalOperation logical = LogicalOperation.AND)
        {
            _logical = logical;
        }

        public abstract string ToString(bool withLogical);
    }

    public class FilterCondition : AbstractCondition
    {
        public override bool IsValid => true;

        public string Column { get; set; }
        public string Value { get; set; }
        protected string _operation;
        public FilterCondition(string column, string value,
                               string operation = "=", LogicalOperation logical = LogicalOperation.AND) : base(logical)
        {
            Column = column;
            Value = value;
            _operation = operation;
        }

        public override string ToString()
        {
            string escape;
            switch (_operation)
            {
                case "LIKE":
                case "ILIKE":
                case "NOT LIKE":
                case "NOT ILIKE":
                    {
                        escape = @" ESCAPE '\'";
                        break;
                    }
                default:
                    {
                        escape = string.Empty;
                        break;
                    }
            }
            return $"{Column} {_operation} '{Value}'{escape}";
        }

        public override string ToString(bool withLogical)
        {
            return withLogical ? $" {_logical.ToString()} {ToString()}" : ToString();
        }
    }

    public class CombinedFilterCondition : AbstractCondition
    {
        public override bool IsValid => Conditions.Any(condition => condition.IsValid);

        private LogicalOperation _combinedLogical;
        public ICollection<AbstractCondition> Conditions { get; }

        public CombinedFilterCondition(LogicalOperation logical = LogicalOperation.AND,
                                       LogicalOperation combinedLogical = LogicalOperation.AND,
                                       params AbstractCondition[] conditions)
            : base(logical)
        {
            _combinedLogical = combinedLogical;
            Conditions = new List<AbstractCondition>(conditions);
        }

        public override string ToString()
        {
            return $"({string.Join($" {_logical.ToString()} ", Conditions.Where(condition => condition?.IsValid ?? false).Select(condition => condition.ToString()))})";
        }

        public override string ToString(bool withLogical)
        {
            return withLogical ? $" {_combinedLogical} {ToString()}" : ToString();
        }
    }
}
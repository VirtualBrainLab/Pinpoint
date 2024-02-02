using System.Collections.Generic;
using System.Text;
using LightJson;
using Unisave.Contracts;
using Unisave.Facades;

namespace Unisave.Heapstore.Backend
{
    /// <summary>
    /// Represents a query request, executable by heapstore
    /// </summary>
    public class QueryRequest
    {
        /// <summary>
        /// Name of the collection to query
        /// </summary>
        public string collection;

        
        #region "Filter clauses"
        
        /// <summary>
        /// List of all defined filter clauses
        /// </summary>
        public List<FilterClause> filterClauses = new List<FilterClause>();

        public class FilterClause
        {
            public const int MaxClauseCount = 20;
            public const int MaxInComparisonItems = 20;
            
            public string field;
            public string op;
            public JsonValue immediateValue;

            public void Validate()
            {
                if (field == null)
                    field = "";
                
                op = NormalizeOperator(op);

                if (op == "IN" || op == "NOT IN")
                {
                    if (immediateValue.AsJsonArray.Count > MaxInComparisonItems)
                    {
                        // ERROR_QUERY_FILTER_IN_ARRAY_TOO_LARGE
                        throw new HeapstoreException(
                            2002,
                            $"The filter clause IN and NOT IN can only compare " +
                            $"against at most {MaxInComparisonItems} values."
                        );
                    }
                }
            }

            public static string NormalizeOperator(string op)
            {
                if (op == null)
                    return "==";
                
                switch (op.ToUpperInvariant().Trim())
                {
                    case "":
                    case "==":
                    case "===":
                        return "==";
                    
                    case "!=":
                    case "!==":
                        return "!=";
                    
                    case ">=": return ">=";
                    case ">": return ">";
                    case "<=": return "<=";
                    case "<": return "<";
                    
                    case "IN": return "IN";
                    case "NOT IN": return "NOT IN";
                }

                // ERROR_QUERY_FILTER_INVALID_OPERATOR
                throw new HeapstoreException(
                    2000, $"The filter clause operator '{op}' is invalid."
                );
            }
        }

        public void ValidateFilterClauses()
        {
            if (filterClauses == null)
                filterClauses = new List<FilterClause>();

            if (filterClauses.Count > FilterClause.MaxClauseCount)
            {
                // ERROR_QUERY_FILTER_TOO_MANY_CLAUSES
                throw new HeapstoreException(
                    2001,
                    $"There may be at most {FilterClause.MaxClauseCount} " +
                    $"filter clauses in a query."
                );
            }

            foreach (var clause in filterClauses)
                clause.Validate();
        }
        
        #endregion
        
        #region "Sort clause"

        /// <summary>
        /// Clause that defines sorting order,
        /// null means no sorting specified
        /// </summary>
        public SortClause sortClause = null;
        
        public class SortClause
        {
            public const int MaxFields = 20;
            
            public List<(string, string)> fieldsAndDirections
                = new List<(string, string)>();

            public void Validate()
            {
                if (fieldsAndDirections == null)
                    fieldsAndDirections = new List<(string, string)>();

                if (fieldsAndDirections.Count > MaxFields)
                {
                    // ERROR_QUERY_SORT_TOO_MANY_FIELDS
                    throw new HeapstoreException(
                        2004,
                        $"There may be at most {MaxFields} " +
                        $"fields in a sort clause."
                    );
                }

                for (int i = 0; i < fieldsAndDirections.Count; i++)
                {
                    (string field, string direction) = fieldsAndDirections[i];

                    if (field == null)
                        field = "";
                    
                    direction = NormalizeDirection(direction);
                    
                    fieldsAndDirections[i] = (field, direction);
                }
            }

            public static string NormalizeDirection(string direction)
            {
                if (string.IsNullOrWhiteSpace(direction))
                    return "ASC";

                switch (direction.ToUpperInvariant().Trim())
                {
                    case "ASC":
                    case "ASCENDING":
                        return "ASC";
                    
                    case "DESC":
                    case "DESCENDING":
                        return "DESC";
                }
                
                // ERROR_QUERY_SORT_INVALID_DIRECTION
                throw new HeapstoreException(
                    2003, $"Invalid sort clause direction '{direction}'"
                );
            }
        }

        public void ValidateSortClause()
        {
            if (sortClause == null)
                return;

            if (sortClause.fieldsAndDirections.Count == 0)
            {
                sortClause = null;
                return;
            }

            sortClause.Validate();
        }
        
        #endregion
        
        #region "Limit clause"

        /// <summary>
        /// Clause that defines limiting (skip and take)
        /// </summary>
        public LimitClause limitClause = new LimitClause();
        
        public class LimitClause
        {
            public const int MaxReturnedDocuments = 1_000;
            
            public int skip = 0; // zero and less means don't skip
            public int take = 0; // zero and less means take all
            
            public void Validate()
            {
                if (skip <= 0)
                    skip = 0;
                
                if (take <= 0)
                    take = 0;

                // limit total number of documents returned by a single query
                if (take > MaxReturnedDocuments || take <= 0)
                    take = MaxReturnedDocuments;
            }
        }

        public void ValidateLimitClause()
        {
            if (limitClause == null)
                limitClause = new LimitClause();
            
            limitClause.Validate();
        }
        
        #endregion
        
        
        ///////////////
        // Execution //
        ///////////////

        public void Validate()
        {
            ValidateFilterClauses();
            ValidateSortClause();
            ValidateLimitClause();
        }
        
        public IAqlQuery BuildAqlQuery()
        {
            // accumulate terms
            var bindings = new Dictionary<string, JsonValue>();
            var sb = new StringBuilder();

            // translate collection selector
            sb.AppendLine("FOR doc IN @@collection");
            bindings["@collection"] = collection;
            
            // translate filter clauses
            for (int i = 0; i < filterClauses.Count; i++)
            {
                var clause = filterClauses[i];
                string aqlOperator = clause.op;
                sb.AppendLine($"FILTER doc[@f_fld_{i}] {aqlOperator} @f_imm_{i}");
                bindings["f_fld_" + i] = clause.field;
                bindings["f_imm_" + i] = clause.immediateValue;
            }
            
            // translate sort clause
            if (sortClause != null)
            {
                sb.Append("SORT ");
                for (int i = 0; i < sortClause.fieldsAndDirections.Count; i++)
                {
                    (string field, string direction) =
                        sortClause.fieldsAndDirections[i];

                    sb.Append(
                        $"doc[@s_fld_{i}] " +
                        (direction == "ASC" ? "ASC" : "DESC")
                    );
                    
                    if (i < sortClause.fieldsAndDirections.Count - 1)
                        sb.Append(", ");
                    
                    bindings[$"s_fld_{i}"] = field;
                }
                sb.AppendLine();
            }
            
            // translate limit clause
            if (limitClause.skip > 0 && limitClause.take > 0)
                sb.AppendLine($"LIMIT {limitClause.skip}, {limitClause.take}");
            else if (limitClause.take > 0)
                sb.AppendLine($"LIMIT {limitClause.take}");
            
            // translate projection (no projection)
            sb.AppendLine("RETURN doc");

            // build the query
            var aqlQuery = DB.Query(sb.ToString());
            foreach (var x in bindings)
                aqlQuery.Bind(x.Key, x.Value);
            return aqlQuery;
        }
    }
}
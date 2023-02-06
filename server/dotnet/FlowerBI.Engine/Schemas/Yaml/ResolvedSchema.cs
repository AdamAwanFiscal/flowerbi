namespace FlowerBI;

using System;
using System.Collections.Generic;
using System.Linq;
using YamlDotNet.Serialization;

public record ResolvedSchema(string Name, string NameInDb, IEnumerable<ResolvedTable> Tables)
{
    public static ResolvedSchema Resolve(string yamlText)
    {
        var deserializer = new DeserializerBuilder().Build();

        var yaml = deserializer.Deserialize<YamlSchema>(yamlText);

        if (string.IsNullOrWhiteSpace(yaml.schema))
        {
            throw new InvalidOperationException("Schema must have non-empty schema property");
        }

        if (yaml.tables == null || !yaml.tables.Any())
        {
            throw new InvalidOperationException("Schema must have non-empty tables property");
        }

        // Validate all columns are [name, type] array
        foreach (var table in yaml.tables)
        {
            if (table.id != null && table.id.Count != 1)
            {
                throw new InvalidOperationException($"Table {table.table} id must have a single column");
            }

            if (table.columns != null)
            {
                foreach (var (name, type) in table.columns.Concat(table.id ?? new Dictionary<string, string[]>()))
                {
                    if (type.Length < 1 || type.Length > 2)
                    {
                        throw new InvalidOperationException($"Table {table.table} column {name} type must be an array of length 1 or 2");
                    }
                }
            }
        }

        var resolvedTables = yaml.tables.Select(t => new ResolvedTable(t.table)).ToList();
        var usedNames = new HashSet<string>();
        
        // Resolve all 'extends' references to get final column lists and DbNames
        var resolutionStack = new HashSet<string>();

        ResolvedTable ResolveExtends(YamlTable table)
        {
            var stackKey = table.table;
            if (!resolutionStack.Add(stackKey))
            {
                var stackString = string.Join(", ", resolutionStack);
                throw new InvalidOperationException($"Circular reference detected: {stackString}");
            }

            var resolvedTable = resolvedTables.FirstOrDefault(x => x.Name == table.table);
            if (resolvedTable.NameInDb == null)
            {
                resolvedTable.IdColumn = table.id != null ? new ResolvedColumn(resolvedTable, table.id.First().Key, table.id.First().Value) : null;
                if (table.columns != null)
                {
                    resolvedTable.Columns.AddRange(table.columns.Select(x => new ResolvedColumn(resolvedTable, x.Key, x.Value)));
                }
                resolvedTable.NameInDb = table.name;

                if (table.extends != null)
                {
                    var extendsYaml = yaml.tables.FirstOrDefault(x => x.table == table.extends);
                    if (extendsYaml == null)
                    {
                        throw new InvalidOperationException($"No such table {table.extends}, referenced in {table.table}");
                    }

                    var extendsTable = ResolveExtends(extendsYaml);

                    resolvedTable.Columns.AddRange(extendsTable.Columns.Select(x => new ResolvedColumn(resolvedTable, x.Name, x.YamlType)
                    {
                        Extends = x
                    }));

                    resolvedTable.IdColumn ??= new ResolvedColumn(resolvedTable, extendsTable.IdColumn.Name, extendsTable.IdColumn.YamlType)
                    {
                        Extends = extendsTable.IdColumn
                    };

                    resolvedTable.NameInDb ??= extendsTable.NameInDb;
                }

                if (resolvedTable.IdColumn == null)
                {
                    throw new InvalidOperationException($"Table {table.table} must have id property (or use 'extends')");
                }

                if (!resolvedTable.Columns.Any())
                {
                    throw new InvalidOperationException($"Table {table.table} must have columns (or use 'extends')");
                }

                resolvedTable.NameInDb ??= table.table;
            }

            resolutionStack.Remove(stackKey);

            return resolvedTable;
        }

        foreach (var table in yaml.tables)
        {
            if (string.IsNullOrWhiteSpace(table.table))
            {
                throw new InvalidOperationException("Table must have non-empty table property");
            }

            if (!usedNames.Add(table.table))
            {
                throw new InvalidOperationException($"More than one table is named '{table.table}'");
            }

            ResolveExtends(table);            
        }

        void ResolveColumnType(ResolvedColumn c)
        {
            var stackKey = $"{c.Table.Name}.{c.Name}";
            if (!resolutionStack.Add(stackKey))
            {
                var stackString = string.Join(", ", resolutionStack);
                throw new InvalidOperationException($"Circular reference detected: {stackString}");
            }

            if (c.DataType == DataType.None)
            {
                var (typeName, nullable) = c.YamlType[0].Last() == '?' ? (c.YamlType[0][0..^1], true) : (c.YamlType[0], false);
                if (Enum.TryParse<DataType>(typeName, true, out var dataType))
                {
                    c.DataType = dataType;
                }
                else
                {
                    var targetColumn = resolvedTables.FirstOrDefault(x => x.Name == typeName)?.IdColumn;
                    if (targetColumn == null)
                    {
                        throw new InvalidOperationException($"{typeName} is neither a data type nor a table, in {c.Table.Name}.{c.Name}");
                    }

                    ResolveColumnType(targetColumn);
                    c.Target = targetColumn;
                    c.DataType = targetColumn.DataType;
                }
                c.NameInDb = c.YamlType.Length == 2 ? c.YamlType[1] : c.Name; 
                c.Nullable = nullable;
            }

            resolutionStack.Remove(stackKey);
        }

        foreach (var table in resolvedTables)
        {
            ResolveColumnType(table.IdColumn);

            foreach (var column in table.Columns)
            {
                ResolveColumnType(column);                    
            }
        }

        return new ResolvedSchema(yaml.schema, yaml.name ?? yaml.schema, resolvedTables);
    }
}

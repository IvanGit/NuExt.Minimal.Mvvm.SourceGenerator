using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.CodeDom.Compiler;
using System.Diagnostics;

namespace Minimal.Mvvm.SourceGenerator
{
    internal struct NotifyDataErrorInfoGenerator
    {
        internal const string NotifyDataErrorInfoAttributeFullyQualifiedName = "Minimal.Mvvm.NotifyDataErrorInfoAttribute";

        #region Pipeline

        internal static bool IsValidSyntaxNode(SyntaxNode attributeTarget, CancellationToken cancellationToken)
        {
            _ = cancellationToken;
            //Trace.WriteLine($"pipeline syntaxNode={attributeTarget}");
            return attributeTarget is ClassDeclarationSyntax;
        }

        public static bool IsValidType(Compilation compilation, ITypeSymbol typeSymbol)
        {
            var baseTypeSymbol = compilation.GetTypeByMetadataName("System.ComponentModel.INotifyDataErrorInfo");
            if (baseTypeSymbol == null || !typeSymbol.ImplementsInterface(baseTypeSymbol))
            {
                return false;
            }

            return true;
        }

        #endregion

        #region Methods

        public static void Generate(IndentedTextWriter writer, IEnumerable<ISymbol> members, Compilation compilation, ref bool isFirst)
        {
            var nullableContextOptions = compilation.Options.NullableContextOptions;
            foreach (var member in members)
            {
                if (member is not ITypeSymbol typeSymbol)
                {
                    Trace.WriteLine($"{member} is not a ITypeSymbol");
                    continue;
                }
                GenerateForMember(writer, typeSymbol, nullableContextOptions, ref isFirst);
            }
        }

        private static void GenerateForMember(IndentedTextWriter writer, ITypeSymbol typeSymbol, NullableContextOptions nullableContextOptions, ref bool isFirst)
        {
            _ = typeSymbol;

            string nullable = nullableContextOptions.HasFlag(NullableContextOptions.Annotations) ? "?" : "";

            var code = GetCodeSource(nullable);
            var lines = GetSourceLines(code);

            if (!isFirst)
            {
                writer.WriteLineNoTabs(string.Empty);
            }
            isFirst = false;

            var originalIndent = writer.Indent;
            try
            {
                foreach (var (indent, length, line) in lines)
                {
                    if (length == 0)
                    {
                        writer.WriteLineNoTabs(string.Empty);
                        continue;
                    }
                    writer.Indent = originalIndent + indent;
                    writer.WriteLine(line);
                }
            }
            finally
            {
                writer.Indent = originalIndent;
            }
        }

        private static List<(int indent, int length, string line)> GetSourceLines(string source)
        {
            var lines = source.Split(s_newLineSeparators, StringSplitOptions.None);
            var (leadingWhitespace, leadingWhitespaceLength) = TextUtils.GetLeadingWhitespace(lines[0]);

            var list = new List<(int indent, int length, string line)>();
            for (int i = 0; i < lines.Length; i++)
            {
                if (leadingWhitespaceLength > 0 && lines[i].StartsWith(leadingWhitespace))
                {
                    lines[i] = lines[i].Substring(leadingWhitespaceLength);
                }
                int indent = 0;
                int length = 0;
                if (!string.IsNullOrWhiteSpace(lines[i]))
                {
                    var spaceCount = TextUtils.GetSpaceCount(lines[i]);
                    Debug.Assert(spaceCount % 4 == 0);
                    indent = spaceCount / 4;
                    lines[i] = lines[i].Trim();
                    length = lines[i].Length;
                }
                list.Add((indent, length, lines[i]));
            }
            return list;
        }

        #endregion

        private static readonly string[] s_newLineSeparators = ["\r\n", "\n"];

        private static string GetCodeSource(string nullable) => $$"""
            private System.Collections.Concurrent.ConcurrentDictionary<string, System.Collections.Generic.List<string>>{{nullable}} _validationErrors;
            private System.Collections.Concurrent.ConcurrentDictionary<string, (System.Threading.Tasks.Task task, System.Threading.CancellationTokenSource cts)>{{nullable}} _validationTasks;

            #region Generated methods for INotifyDataErrorInfo validation

            /// <summary>
            /// Cancels the validation task for the specified property.
            /// </summary>
            /// <param name="propertyName">
            /// The name of the property. This parameter may be set by the compiler if called from within a property setter,
            /// or it should be specified explicitly.
            /// </param>
            public void CancelValidationTask([System.Runtime.CompilerServices.CallerMemberName] string{{nullable}} propertyName = null)
            {
                if (propertyName is not { Length: > 0 } || _validationTasks is null || !_validationTasks.TryRemove(propertyName, out var validation))
                {
                    return;
                }
                var (task, cts) = validation;
                try
                {
                    if (!task.IsCompleted)
                    {
                        cts.Cancel();
                    }
                }
                catch (System.ObjectDisposedException)
                {
                    //do nothing
                }
            }

            /// <summary>
            /// Cancels all ongoing validation tasks.
            /// </summary>
            /// <remarks>
            /// It is recommended to call this method during the uninitialization or disposal of the model/object
            /// to ensure that no lingering validation tasks remain active.
            /// </remarks>
            public void CancelAllValidationTasks()
            {
                if (_validationTasks == null)
                {
                    return;
                }
                System.Collections.Generic.List<System.AggregateException>{{nullable}} exceptions = null;
                foreach (var pair in _validationTasks)
                {
                    var (task, cts) = pair.Value;
                    try
                    {
                        if (!task.IsCompleted)
                        {
                            cts.Cancel();
                        }
                    }
                    catch (System.ObjectDisposedException)
                    {
                        //do nothing
                    }
                    catch (System.AggregateException ex)
                    {
                        (exceptions ??= []).Add(ex);
                    }
                }
                _validationTasks.Clear();
                if (exceptions is not null)
                {
                    throw new System.AggregateException(exceptions);
                }
            }

            /// <summary>
            /// Clears validation errors for the specified property.
            /// </summary>
            /// <param name="propertyName">
            /// The name of the property. This parameter may be set by the compiler if called from within a property setter,
            /// or it should be specified explicitly.
            /// </param>
            public void ClearErrors([System.Runtime.CompilerServices.CallerMemberName] string{{nullable}} propertyName = null)
            {
                if (propertyName is not { Length: > 0 } || _validationErrors is null || !_validationErrors.TryGetValue(propertyName, out var errors)) return;
                errors.Clear();
                OnErrorsChanged(propertyName);
            }

            /// <summary>
            /// Clears all validation errors for all properties.
            /// </summary>
            public void ClearAllErrors()
            {
                if (_validationErrors is not { Count: > 0 }) return;
                foreach (var pair in _validationErrors)
                {
                    ClearErrors(pair.Key);
                }
                OnErrorsChanged(null);
            }

            /// <summary>
            /// Triggers the <see cref="ErrorsChanged"/> event for the specified property.
            /// </summary>
            /// <param name="propertyName">
            /// The name of the property. This parameter may be set by the compiler if called from within a property setter,
            /// or it should be specified explicitly.
            /// </param>
            private void OnErrorsChanged([System.Runtime.CompilerServices.CallerMemberName] string{{nullable}} propertyName = null)
            {
                ErrorsChanged?.Invoke(this, new System.ComponentModel.DataErrorsChangedEventArgs(propertyName));
            }

            /// <summary>
            /// Sets an error message for the specified property.
            /// </summary>
            /// <param name="error">The error message.</param>
            /// <param name="propertyName">
            /// The name of the property. This parameter may be set by the compiler if called from within a property setter,
            /// or it should be specified explicitly.
            /// </param>
            public void SetError(string error, [System.Runtime.CompilerServices.CallerMemberName] string{{nullable}} propertyName = null)
            {
                if (propertyName is not { Length: > 0 })
                {
                    return;
                }

                (_validationErrors ??= []).AddOrUpdate(
                    propertyName,
                    addValueFactory: key => [error],
                    updateValueFactory: (key, errors) =>
                    {
                        errors.Add(error);
                        return errors;
                    });

                OnErrorsChanged(propertyName);
            }

            /// <summary>
            /// Sets a validation task for the specified property.
            /// </summary>
            /// <param name="task">The validation task.</param>
            /// <param name="cts">The cancellation token source for the task.</param>
            /// <param name="propertyName">
            /// The name of the property. This parameter may be set by the compiler if called from within a property setter,
            /// or it should be specified explicitly.
            /// </param>
            public void SetValidationTask(System.Threading.Tasks.Task task, System.Threading.CancellationTokenSource cts, string propertyName)
            {
                if (propertyName is not { Length: > 0 })
                {
                    return;
                }

                if (_validationTasks?.TryRemove(propertyName, out var oldValidation) == true)
                {
                    var (_, oldCts) = oldValidation;
                    try
                    {
                        oldCts.Cancel();
                    }
                    catch (System.ObjectDisposedException)
                    {
                        //do nothing
                    }
                }

                (_validationTasks ??= [])[propertyName] = (task, cts);
            }

            #endregion

            #region INotifyDataErrorInfo

            /// <summary>
            /// Occurs when the validation errors have changed for a property or for the entire object.
            /// </summary>
            public event System.EventHandler<System.ComponentModel.DataErrorsChangedEventArgs>{{nullable}} ErrorsChanged;
            
            System.Collections.IEnumerable System.ComponentModel.INotifyDataErrorInfo.GetErrors(string{{nullable}} propertyName)
            {
                if (propertyName is not { Length: > 0 } || _validationErrors is null || !_validationErrors.TryGetValue(propertyName, out var errors))
                {
                    return System.Array.Empty<string>();
                }
                return errors;
            }

            /// <summary>
            /// Gets a value that indicates whether the entity has validation errors.
            /// </summary>
            public bool HasErrors => _validationErrors != null && System.Linq.Enumerable.Any(_validationErrors, (pair => pair.Value is { Count: > 0 }));

            #endregion
            """;
    }
}

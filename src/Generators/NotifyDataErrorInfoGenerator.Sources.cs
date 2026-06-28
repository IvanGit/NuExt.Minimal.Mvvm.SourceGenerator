namespace Minimal.Mvvm.SourceGenerator;

partial struct NotifyDataErrorInfoGenerator
{
    private static string GetCodeSource(string nullable, string eventArgsCache, string dataErrorsChangedEventArgsCache) => $$"""
        #region INotifyDataErrorInfo validation

        private global::System.Collections.Concurrent.ConcurrentDictionary<string, global::System.Collections.Generic.List<string>>{{nullable}} _validationErrors;
        private global::System.Collections.Concurrent.ConcurrentDictionary<string, (global::System.Threading.Tasks.Task task, global::System.Threading.CancellationTokenSource cts)>{{nullable}} _validationTasks;
        private global::System.Threading.SynchronizationContext{{nullable}} _ui;
        private global::System.Threading.SendOrPostCallback{{nullable}} _errorsChanged;
        private global::System.Threading.SendOrPostCallback{{nullable}} _hasErrorsChanged;
        private volatile int _hasErrors;

#if !NET8_0_OR_GREATER
        private static readonly global::System.Collections.Generic.IReadOnlyDictionary<string, string[]> s_emptyErrorsMap =
            new global::System.Collections.ObjectModel.ReadOnlyDictionary<string, string[]>(
                new global::System.Collections.Generic.Dictionary<string, string[]>(0, global::System.StringComparer.Ordinal));
#endif

        /// <summary>
        /// Associates an asynchronous validation task with the specified property, replacing any previous task.
        /// </summary>
        /// <param name="task">The validation task to track.</param>
        /// <param name="cts">The cancellation token source linked to <paramref name="task"/>.</param>
        /// <param name="propertyName">
        /// The name of the property initiating the validation; this value is normally supplied
        /// by the compiler when called from a property setter.
        /// </param>
        /// <remarks>
        /// If an existing task is present for <paramref name="propertyName"/>, it is cancelled and disposed
        /// before the new task is stored.
        /// </remarks>
        public void SetValidationTask(global::System.Threading.Tasks.Task task, global::System.Threading.CancellationTokenSource cts, [global::System.Runtime.CompilerServices.CallerMemberName] string{{nullable}} propertyName = null)
        {
            if (propertyName is null)
            {
                return;
            }

            if (_validationTasks?.TryRemove(propertyName, out var oldValidation) == true)
            {
                var (oldTask, oldCts) = oldValidation;
                try
                {
                    if (!oldTask.IsCompleted)
                    {
                        oldCts.Cancel();
                    }
                    oldCts.Dispose();
                }
                catch (global::System.ObjectDisposedException)
                {
                    //do nothing
                }
                catch (global::System.AggregateException)
                {
                    //do nothing
                }
            }

            (_validationTasks ??= new())[propertyName] = (task, cts);
        }

        /// <summary>
        /// Cancels the validation task for the specified property; cancels all tracked validation tasks when <paramref name="propertyName"/> is <c>null</c> or empty.
        /// </summary>
        /// <param name="propertyName">
        /// The name of the property. When <c>null</c> or empty, all tasks are cancelled and cleared.
        /// </param>
        /// <remarks>
        /// This method cancels and disposes the associated <see cref="global::System.Threading.CancellationTokenSource"/> instances
        /// and clears the internal task registry accordingly.
        /// </remarks>
        public void CancelValidationTask([global::System.Runtime.CompilerServices.CallerMemberName] string{{nullable}} propertyName = null)
        {
            var validationTasks = _validationTasks;
            if (validationTasks == null || validationTasks.IsEmpty)
            {
                return;
            }
            if (propertyName is not { Length: > 0 })
            {
                foreach (var pair in validationTasks)
                {
                    var (task, cts) = pair.Value;
                    try
                    {
                        if (!task.IsCompleted)
                        {
                            cts.Cancel();
                        }
                        cts.Dispose();
                    }
                    catch (global::System.ObjectDisposedException)
                    {
                        //do nothing
                    }
                    catch (global::System.AggregateException)
                    {
                        //do nothing
                    }
                }
                validationTasks.Clear();
            }
            else
            {
                if (!validationTasks.TryRemove(propertyName, out var validation))
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
                    cts.Dispose();
                }
                catch (global::System.ObjectDisposedException)
                {
                    //do nothing
                }
                catch (global::System.AggregateException)
                {
                    //do nothing
                }
            }
        }

        /// <summary>
        /// Cancels all ongoing validation tasks.
        /// </summary>
        /// <remarks>
        /// Recommended to invoke during object teardown to ensure no background validation remains active.
        /// </remarks>
        public void CancelAllValidationTasks() => CancelValidationTask(null);

        /// <summary>
        /// Adds a single validation error for the specified property; clears errors for that scope when <paramref name="message"/> is <c>null</c> or empty.
        /// </summary>
        /// <param name="message">The validation error message. When <c>null</c> or empty, errors for the scope are cleared.</param>
        /// <param name="propertyName">
        /// The name of the property; when <c>null</c> or empty, the operation targets entity-level errors.
        /// </param>
        /// <remarks>
        /// The operation is idempotent with respect to existing messages. Internal storage uses copy-on-write to avoid
        /// concurrent enumeration issues in UI bindings.
        /// </remarks>
        public void SetError(string message, [global::System.Runtime.CompilerServices.CallerMemberName] string{{nullable}} propertyName = null)
        {
            if (message is not { Length: > 0 })
            {
                ClearErrors(propertyName);
                return;
            }

            propertyName ??= string.Empty;// null or "" => entity-level

            var validationErrors = _validationErrors ??= new(global::System.StringComparer.Ordinal);
            global::System.Collections.Generic.List<string>{{nullable}} errors = null;
            while (true)
            {
                if (validationErrors.TryGetValue(propertyName, out var current))
                {
                    if (current.Contains(message))
                    {
                        return;// no change
                    }
                    var copy = new global::System.Collections.Generic.List<string>(current.Count + 1);
                    copy.AddRange(current);
                    copy.Add(message);
                    if (validationErrors.TryUpdate(propertyName, copy, current))
                    {
                        break;
                    }
                    continue;
                }
                else
                {
                    errors ??= [message];
                    if (validationErrors.TryAdd(propertyName, errors))
                    {
                        break;
                    }
                }
            }

            bool raiseHasErrorsChanged = UpdateHasErrors();
            OnErrorsChanged(raiseHasErrorsChanged, propertyName);
        }

        /// <summary>
        /// Merges the specified validation messages into the target scope; clears errors when <paramref name="messages"/> is <c>null</c> or contains no items.
        /// </summary>
        /// <param name="messages">The validation messages to add. Duplicate and empty entries are ignored.</param>
        /// <param name="propertyName">
        /// The name of the property; when <c>null</c> or empty, messages apply to the entity-level scope.
        /// </param>
        /// <remarks>
        /// This method does not remove existing messages unless <paramref name="messages"/> is <c>null</c>
        /// or reduces to an empty set, in which case errors for the scope are cleared.
        /// </remarks>
        public void SetErrors(global::System.Collections.Generic.IEnumerable<string>{{nullable}} messages, [global::System.Runtime.CompilerServices.CallerMemberName] string{{nullable}} propertyName = null)
        {
            if (messages is null)
            {
                ClearErrors(propertyName);
                return;
            }

            global::System.Collections.Generic.HashSet<string>{{nullable}} unique = null;
            global::System.Collections.Generic.List<string>{{nullable}} ordered = null;
            foreach (var msg in messages)
            {
                if (!string.IsNullOrEmpty(msg))
                {
                    unique ??= new(global::System.StringComparer.Ordinal);
                    if (unique.Add(msg))
                    {
                        (ordered ??= new()).Add(msg);
                    }
                }
            }

            if (unique is null || unique.Count == 0)
            {
                ClearErrors(propertyName);
                return;
            }

            propertyName ??= string.Empty;// null or "" => entity-level

            var validationErrors = _validationErrors ??= new(global::System.StringComparer.Ordinal);
            global::System.Collections.Generic.List<string>{{nullable}} errors = null;
            while (true)
            {
                if (validationErrors.TryGetValue(propertyName, out var current))
                {
                    var currentSet = new global::System.Collections.Generic.HashSet<string>(current, global::System.StringComparer.Ordinal);
                    if (currentSet.IsSupersetOf(unique))
                    {
                        return;// no change
                    }

                    var copy = new global::System.Collections.Generic.List<string>(checked(current.Count + unique.Count));
                    copy.AddRange(current);
                    foreach (var msg in ordered!)
                    {
                        if (!currentSet.Contains(msg))
                        {
                            copy.Add(msg);
                        }
                    }

                    if (validationErrors.TryUpdate(propertyName, copy, current))
                    {
                        break;
                    }
                    // CAS failed — retry.
                    continue;
                }
                else
                {
                    errors ??= new(ordered!);
                    if (validationErrors.TryAdd(propertyName, errors))
                    {
                        break;
                    }
                    // CAS failed — retry.
                }
            }

            bool raiseHasErrorsChanged = UpdateHasErrors();
            OnErrorsChanged(raiseHasErrorsChanged, propertyName);
        }

        /// <summary>
        /// Replaces the current validation messages for the specified scope with the given set.
        /// Passing <c>null</c> or an empty sequence clears errors.
        /// </summary>
        /// <param name="messages">The validation messages to set. Duplicate and empty entries are ignored.</param>
        /// <param name="propertyName">
        /// The name of the property; when <c>null</c> or empty, messages apply to the entity-level scope.
        /// </param>
        /// <remarks>
        /// Performs a copy-on-write update; raises <see cref="ErrorsChanged"/> only when the target scope actually changes.
        /// </remarks>
        public void ReplaceErrors(global::System.Collections.Generic.IEnumerable<string>{{nullable}} messages, [global::System.Runtime.CompilerServices.CallerMemberName] string{{nullable}} propertyName = null)
        {
            if (messages is null)
            {
                ClearErrors(propertyName);
                return;
            }

            global::System.Collections.Generic.HashSet<string>{{nullable}} unique = null;
            global::System.Collections.Generic.List<string>{{nullable}} ordered = null;
            foreach (var msg in messages)
            {
                if (!string.IsNullOrEmpty(msg))
                {
                    unique ??= new(global::System.StringComparer.Ordinal);
                    if (unique.Add(msg))
                    {
                        (ordered ??= new()).Add(msg);
                    }
                }
            }

            if (unique is null || unique.Count == 0)
            {
                ClearErrors(propertyName);
                return;
            }

            propertyName ??= string.Empty;// null or "" => entity-level

            var validationErrors = _validationErrors ??= new(global::System.StringComparer.Ordinal);
            global::System.Collections.Generic.List<string>{{nullable}} errors = null;
            while (true)
            {
                if (validationErrors.TryGetValue(propertyName, out var current))
                {
                    if (current.Count == unique.Count)
                    {
                        var currentSet = new global::System.Collections.Generic.HashSet<string>(current, global::System.StringComparer.Ordinal);
                        if (currentSet.SetEquals(unique))
                        {
                            return;// no change
                        }
                    }

                    errors ??= new(ordered!);
                    if (validationErrors.TryUpdate(propertyName, errors, current))
                    {
                        break;
                    }
                    // CAS failed — retry.
                    continue;
                }
                else
                {
                    errors ??= new(ordered!);
                    if (validationErrors.TryAdd(propertyName, errors))
                    {
                        break;
                    }
                    // CAS failed — retry.
                }
            }

            bool raiseHasErrorsChanged = UpdateHasErrors();
            OnErrorsChanged(raiseHasErrorsChanged, propertyName);
        }

        /// <summary>
        /// Removes a validation message from the specified scope, if present.
        /// </summary>
        /// <param name="message">The validation error message.</param>
        /// <param name="propertyName">
        /// The name of the property; when <c>null</c> or empty, the operation targets entity-level errors.
        /// </param>
        /// <returns>
        /// <c>true</c> if the message was removed and the internal state changed; otherwise, <c>false</c>.
        /// </returns>
        public bool RemoveError(string message, [global::System.Runtime.CompilerServices.CallerMemberName] string{{nullable}} propertyName = null)
        {
            if (string.IsNullOrEmpty(message))
            {
                return false;
            }

            var validationErrors = _validationErrors;
            if (validationErrors is null)
            {
                return false;
            }

            propertyName ??= string.Empty;// null or "" => entity-level

            while (true)
            {
                if (!validationErrors.TryGetValue(propertyName, out var current) || current.Count == 0)
                {
                    return false;
                }
                if (!current.Contains(message))
                {
                    return false;
                }
                if (current.Count == 1)
                {
                    if (validationErrors.TryRemove(propertyName, out _))
                    {
                        break;
                    }
                    continue;
                }
                var copy = new global::System.Collections.Generic.List<string>(current.Count - 1);
                foreach (var msg in current)
                {
                    if (!global::System.StringComparer.Ordinal.Equals(msg, message))
                    {
                        copy.Add(msg);
                    }
                }
                if (validationErrors.TryUpdate(propertyName, copy, current))
                {
                    break;
                }
            }

            bool raiseHasErrorsChanged = UpdateHasErrors();
            OnErrorsChanged(raiseHasErrorsChanged, propertyName);
            return true;
        }

        /// <summary>
        /// Clears validation errors for the specified scope.
        /// </summary>
        /// <param name="propertyName">
        /// The property name; when <c>null</c> or empty, clears entity-level errors.
        /// </param>
        public void ClearErrors([global::System.Runtime.CompilerServices.CallerMemberName] string{{nullable}} propertyName = null)
        {
            var validationErrors = _validationErrors;
            if (validationErrors is null || validationErrors.IsEmpty)
            {
                return;
            }

            propertyName ??= string.Empty;// null or "" => entity-level

            if (validationErrors.TryRemove(propertyName, out _))
            {
                bool raiseHasErrorsChanged = UpdateHasErrors();
                OnErrorsChanged(raiseHasErrorsChanged, propertyName);
            }
        }

        /// <summary>
        /// Clears all validation errors for the current instance.
        /// </summary>
        public void ClearAllErrors()
        {
            var validationErrors = _validationErrors;
            if (validationErrors is null || validationErrors.IsEmpty)
            {
                return;
            }

            global::System.Collections.Generic.List<string>{{nullable}} properties = null;
            foreach (var pair in validationErrors)
            {
                if (pair.Value.Count == 0)
                {
                    continue;
                }
                (properties ??= new()).Add(pair.Key);
            }

            validationErrors.Clear();

            bool raiseHasErrorsChanged = UpdateHasErrors();
            if (properties is { })
            {
                foreach (var prop in properties)
                {
                    OnErrorsChanged(false, prop);
                }
            }

            if (raiseHasErrorsChanged)
            {
                OnHasErrorsChanged();
            }
        }

        /// <summary>
        /// Gets a snapshot of validation error messages for the specified property or for the entity.
        /// </summary>
        /// <param name="propertyName">
        /// The name of the property to retrieve errors for; when <c>null</c> or empty, returns entity-level errors.
        /// </param>
        /// <returns>
        /// A snapshot (copy) of the current validation messages for the requested scope. Returns an empty sequence if none are present.
        /// </returns>
        public global::System.Collections.Generic.IEnumerable<string> GetErrors(string{{nullable}} propertyName = null)
        {
            var validationErrors = _validationErrors;
            if (validationErrors is null || validationErrors.IsEmpty)
            {
                return [];
            }

            propertyName ??= string.Empty;// null or "" => entity-level

            if (!validationErrors.TryGetValue(propertyName, out var errors) || errors.Count == 0)
            {
                return [];
            }
            return errors.ToArray();
        }

        /// <summary>
        /// Returns a snapshot of all current validation messages grouped by property name,
        /// including the entity-level key (<see cref="string.Empty"/>).
        /// </summary>
        public global::System.Collections.Generic.IReadOnlyDictionary<string, string[]> GetErrorsSnapshot()
        {
            var validationErrors = _validationErrors;
            if (validationErrors is null || validationErrors.IsEmpty)
            {
#if NET8_0_OR_GREATER
                return global::System.Collections.Frozen.FrozenDictionary<string, string[]>.Empty;
#else
                return s_emptyErrorsMap;
#endif
            }
            var copy = new global::System.Collections.Generic.Dictionary<string, string[]>(validationErrors.Count, global::System.StringComparer.Ordinal);
            foreach (var pair in validationErrors)
            {
                var list = pair.Value;
                if (list.Count > 0)
                {
                    copy[pair.Key] = list.ToArray();
                }
            }

#if NET8_0_OR_GREATER
            return copy.Count == 0 ? global::System.Collections.Frozen.FrozenDictionary<string, string[]>.Empty :
                global::System.Collections.Frozen.FrozenDictionary.ToFrozenDictionary(copy, global::System.StringComparer.Ordinal);
#else
            return copy.Count == 0 ? s_emptyErrorsMap :
                new global::System.Collections.ObjectModel.ReadOnlyDictionary<string, string[]>(copy);
#endif

        }

        /// <summary>
        /// Returns whether the specified scope currently has validation errors.
        /// </summary>
        /// <param name="propertyName">
        /// The name of the property to check; when <c>null</c> or empty, checks the entity-level scope.
        /// </param>
        /// <returns><c>true</c> if the scope currently has errors; otherwise, <c>false</c>.</returns>
        public bool HasErrorsFor([global::System.Runtime.CompilerServices.CallerMemberName] string{{nullable}} propertyName = null)
        {
            var validationErrors = _validationErrors;
            if (validationErrors is null || validationErrors.IsEmpty)
            {
                return false;
            }

            propertyName ??= string.Empty;// null or "" => entity-level

            return validationErrors.TryGetValue(propertyName, out var errors) && errors.Count > 0;
        }

        /// <summary>
        /// Caches a non-null current <see cref="System.Threading.SynchronizationContext"/> for UI dispatching.
        /// </summary>
        /// <param name="current">Receives the current context snapshot.</param>
        private void EnsureSynchronizationContext(ref global::System.Threading.SynchronizationContext{{nullable}} current)
        {
            current = global::System.Threading.SynchronizationContext.Current;
            if (_ui is null && current is not null)
            {
                _ui = current;
            }
        }

        /// <summary>
        /// Raises <see cref="ErrorsChanged"/> for the specified scope. If invoked from a non-UI thread
        /// and a UI context was captured, the notification is dispatched to that context.
        /// </summary>
        /// <param name="raiseHasErrorsChanged">
        /// Indicates whether <see cref="global::System.ComponentModel.INotifyDataErrorInfo.HasErrors"/> has changed
        /// and a corresponding <see cref="global::System.ComponentModel.INotifyPropertyChanged.PropertyChanged"/> notification should be raised.
        /// </param>
        /// <param name="propertyName">
        /// The name of the property; <c>null</c> or empty indicates the entity-level scope.
        /// </param>
        private void OnErrorsChanged(bool raiseHasErrorsChanged, [global::System.Runtime.CompilerServices.CallerMemberName] string{{nullable}} propertyName = null)
        {
            var handler = errorsChanged;
            if (handler is null)
            {
                if (raiseHasErrorsChanged)
                {
                    OnHasErrorsChanged();
                }
                return;
            }

            global::System.Threading.SynchronizationContext{{nullable}} current = null;
            EnsureSynchronizationContext(ref current);

            var args = {{dataErrorsChangedEventArgsCache}}.Get(propertyName);
            if (_ui is not null && current != _ui)
            {
                _ui.Post(_errorsChanged ??= OnErrorsChanged, (args, raiseHasErrorsChanged));
                return;
            }

            handler(this, args);
            if (raiseHasErrorsChanged)
            {
                OnPropertyChanged({{eventArgsCache}}.HasErrorsPropertyChanged);
            }
        }

        [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
        private void OnHasErrorsChanged()
        {
            global::System.Threading.SynchronizationContext{{nullable}} current = null;
            EnsureSynchronizationContext(ref current);

            if (_ui is not null && current != _ui)
            {
                _ui.Post(_hasErrorsChanged ??= OnHasErrorsChanged, null);
                return;
            }

            OnPropertyChanged({{eventArgsCache}}.HasErrorsPropertyChanged);
        }

        [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
        private void OnErrorsChanged(object{{nullable}} state)
        {
            var (args, raiseHasErrorsChanged) = ((global::System.ComponentModel.DataErrorsChangedEventArgs, bool))state!;
            errorsChanged?.Invoke(this, args);
            if (raiseHasErrorsChanged)
            {
                OnPropertyChanged({{eventArgsCache}}.HasErrorsPropertyChanged);
            }
        }

        [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
        private void OnHasErrorsChanged(object{{nullable}} state)
        {
            OnPropertyChanged({{eventArgsCache}}.HasErrorsPropertyChanged);
        }

        [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
        private bool UpdateHasErrors()
        {
            int hasErrors = 0;
            var validationErrors = _validationErrors;
            if (validationErrors is not null && !validationErrors.IsEmpty)
            {
                foreach (var pair in validationErrors)
                {
                    if (pair.Value.Count > 0)
                    {
                        hasErrors = 1;
                        break;
                    }
                }
            }
            var oldValue = _hasErrors;
            if (oldValue != hasErrors)
            {
                _hasErrors = hasErrors;
                return true;
            }
            return false;
        }

        #endregion

        #region INotifyDataErrorInfo implementation

        private event global::System.EventHandler<System.ComponentModel.DataErrorsChangedEventArgs>{{nullable}} errorsChanged;

        /// <inheritdoc />
        public event global::System.EventHandler<global::System.ComponentModel.DataErrorsChangedEventArgs>{{nullable}} ErrorsChanged
        {
            add
            {
                global::System.Threading.SynchronizationContext{{nullable}} current = null;
                EnsureSynchronizationContext(ref current);
                errorsChanged += value;
            }
            remove
            {
                errorsChanged -= value;
            }
        }

        /// <inheritdoc />
        global::System.Collections.IEnumerable global::System.ComponentModel.INotifyDataErrorInfo.GetErrors(string{{nullable}} propertyName)
            => GetErrors(propertyName);

        /// <inheritdoc />
        public bool HasErrors => _hasErrors == 1;

        #endregion
""";
}

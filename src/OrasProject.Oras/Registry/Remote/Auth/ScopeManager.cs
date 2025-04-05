﻿// Copyright The ORAS Authors.
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace OrasProject.Oras.Registry.Remote.Auth;

public class ScopeManager
{
    private static Lazy<ScopeManager> _instance = new(() => new ScopeManager());

    /// <summary>
    /// A thread-safe, lazily initialized singleton instance of the <see cref="ScopeManager"/> class.
    /// </summary>
    public static ScopeManager Instance => _instance.Value;
    
    /// <summary>
    /// A thread-safe dictionary that maps a string key to a sorted set of <see cref="Scope"/> objects.
    /// This is used to manage and organize scopes in a concurrent environment.
    /// </summary>
    private ConcurrentDictionary<string, SortedSet<Scope>> Scopes { get; } = new ();
    
    /// <summary>
    /// ResetInstanceForTesting resets the _instance for testing purpose
    /// </summary>
    internal static void ResetInstanceForTesting()
    {
        _instance = new Lazy<ScopeManager>(() => new ScopeManager());
    }
    
    /// <summary>
    /// GetScopesForHost returns a sorted set of scopes for the given registry if found,
    /// otherwise, returns empty sorted set.
    /// </summary>
    /// <param name="registry"></param>
    /// <returns></returns>
    public SortedSet<Scope> GetScopesForHost(string registry)
    {
        return Scopes.TryGetValue(registry, out var scopes) ? scopes : new();
    }
    
    /// <summary>
    /// GetScopesStringForHost returns a list of scopes string for the given registry if found,
    /// otherwise, returns empty list.
    /// </summary>
    /// <param name="registry"></param>
    /// <returns></returns>
    public List<string> GetScopesStringForHost(string registry)
    {
        return Scopes.TryGetValue(registry, out var scopes) 
            ? scopes.Select(scope => scope.ToString()).ToList() 
            : new ();
    }

    /// <summary>
    /// SetScopeForRegistry sets the scope for a specific registry. If the scope contains the "All" action, 
    /// it ensures that only the "All" action is retained. Otherwise, it merges the actions 
    /// of the provided scope with any existing scope for the registry.
    /// </summary>
    /// <param name="registry">The registry for which the scope is being set.</param>
    /// <param name="scope">The scope to be set for the registry, including its actions.</param>
    public void SetScopeForRegistry(string registry, Scope scope)
    {
        if (scope.Actions.Contains(Scope.Action.All))
        {
            scope.Actions.Clear();
            scope.Actions.Add(Scope.Action.All);
        }

        Scopes.AddOrUpdate(registry,
            new SortedSet<Scope>{ scope },
            (_, existingScopes) =>
            {
                if (existingScopes.TryGetValue(scope, out var existingScope))
                {
                    if (existingScope.Actions.Contains(Scope.Action.All) || scope.Actions.Contains(Scope.Action.All))
                    {
                        existingScope.Actions.Clear();
                        existingScope.Actions.Add(Scope.Action.All);
                    }
                    else
                    {
                        existingScope.Actions.UnionWith(scope.Actions);
                    }
                }
                else
                {
                    existingScopes.Add(scope);
                }

                return existingScopes;
            });
    }
}

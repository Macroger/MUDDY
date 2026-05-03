// Copyright 2026 Matthew Schatz
// SPDX-License-Identifier: Apache-2.0
using Microsoft.VisualStudio.TestTools.UnitTesting;

[assembly: Parallelize(Workers = 0, Scope = ExecutionScope.MethodLevel)]

﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNet.WebHooks.Config
{
    /// <summary>
    /// This class provides configuration information for connection strings.
    /// </summary>
    public class ConnectionSettings
    {
        private string _name;
        private string _connectionString;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectionSettings"/> with a given <paramref name="name"/>
        /// and <paramref name="connectionString"/>.
        /// </summary>
        /// <param name="name">The name of the connection string setting.</param>
        /// <param name="connectionString">The actual connection string.</param>
        public ConnectionSettings(string name, string connectionString)
        {
            _name = name ?? throw new ArgumentNullException(nameof(name));
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        /// <summary>
        /// Gets or sets the name of the connection string.
        /// </summary>
        public string Name
        {
            get => _name;

            set => _name = value ?? throw new ArgumentNullException(nameof(value));
        }

        /// <summary>
        /// Gets or sets the actual connection string.
        /// </summary>
        public string ConnectionString
        {
            get => _connectionString;

            set => _connectionString = value ?? throw new ArgumentNullException(nameof(value));
        }

        /// <summary>
        /// Gets or sets the provider to be used by this connection string, e.g. <c>System.Data.SqlClient</c>.
        /// </summary>
        public string Provider { get; set; }
    }
}
